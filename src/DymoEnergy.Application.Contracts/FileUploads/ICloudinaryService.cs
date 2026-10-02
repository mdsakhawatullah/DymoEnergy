using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.FileUploads;

public interface ICloudinaryService : ITransientDependency
{
    /// <summary>
    /// Uploads an image file to Cloudinary
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">The name of the file</param>
    /// <param name="folder">Optional folder name in Cloudinary</param>
    /// <returns>The secure URL of the uploaded image</returns>
    Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder = "DymoEnergy");

    /// <summary>
    /// Uploads a document (PDF, Office file, image, zip) as a raw Cloudinary asset and returns its secure URL.
    /// </summary>
    Task<string> UploadDocumentAsync(Stream fileStream, string fileName, string folder = "DymoEnergy/documents");

    /// <summary>
    /// Deletes an image from Cloudinary
    /// </summary>
    /// <param name="publicId">The public ID of the image to delete</param>
    Task DeleteImageAsync(string publicId);
}
