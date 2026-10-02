using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace DymoEnergy.Compliance;

public class ComplianceArchiveService : IComplianceArchiveService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly IRepository<ComplianceFile, int>        _files;
    private readonly IRepository<ComplianceLicence, int>     _licences;
    private readonly IRepository<ComplianceCertificate, int> _certificates;
    private readonly IRepository<ComplianceListItem, int>    _items;
    private readonly IAuthorizationService                   _authorization;

    public ComplianceArchiveService(
        IRepository<ComplianceFile, int> files,
        IRepository<ComplianceLicence, int> licences,
        IRepository<ComplianceCertificate, int> certificates,
        IRepository<ComplianceListItem, int> items,
        IAuthorizationService authorization)
    {
        _files = files; _licences = licences; _certificates = certificates; _items = items; _authorization = authorization;
    }

    [UnitOfWork]
    public virtual async Task<Stream?> BuildArchiveAsync()
    {
        if (!(await _authorization.AuthorizeAsync(DymoEnergyPermissions.Compliance.Default)).Succeeded)
            throw new AbpAuthorizationException();

        var files = await _files.GetListAsync();
        if (files.Count == 0) return null;

        var names = new Dictionary<(ComplianceFileOwner, int), string>();
        foreach (var l in await _licences.GetListAsync())     names[(ComplianceFileOwner.Licence, l.Id)] = l.Name;
        foreach (var c in await _certificates.GetListAsync()) names[(ComplianceFileOwner.Certificate, c.Id)] = c.ProductName;
        foreach (var i in await _items.GetListAsync())
        {
            if (i.Kind == ComplianceItemKind.Template)    names[(ComplianceFileOwner.Template, i.Id)] = i.Title;
            if (i.Kind == ComplianceItemKind.ImportPaper) names[(ComplianceFileOwner.ImportPaper, i.Id)] = i.Title;
        }

        var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                byte[] bytes;
                try { bytes = await Http.GetByteArrayAsync(f.Url); }
                catch (HttpRequestException) { continue; }   // a file that vanished from storage must not break the whole archive
                catch (TaskCanceledException) { continue; }

                names.TryGetValue((f.OwnerKind, f.OwnerId), out var owner);
                var path = $"{f.OwnerKind}/{Safe(owner ?? "item-" + f.OwnerId)}/{Safe(f.FileName)}";
                var unique = path;
                for (var n = 2; !used.Add(unique); n++)
                    unique = $"{Path.GetDirectoryName(path)?.Replace('\\', '/')}/{Path.GetFileNameWithoutExtension(path)} ({n}){Path.GetExtension(path)}";

                var entry = zip.CreateEntry(unique, CompressionLevel.Fastest);
                await using var es = entry.Open();
                await es.WriteAsync(bytes);
            }
        }
        output.Position = 0;
        return output;
    }

    private static string Safe(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}
