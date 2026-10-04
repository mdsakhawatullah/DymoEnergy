import { CourierProvider } from '../../proxy/shipping/models';

const BASE = 'assets/images/couriers/';

/** Round mark, for small badges. Null when there is no logo for the courier. */
export function courierMark(provider: CourierProvider | null | undefined): string | null {
  return provider === CourierProvider.Pathao ? BASE + 'pathao-mark.png' : null;
}

/** Full logo with the name, for wide spots. */
export function courierLogo(provider: CourierProvider | null | undefined): string | null {
  return provider === CourierProvider.Pathao ? BASE + 'pathao.png' : null;
}
