export const TAGS = {
  delivery: 'Delivery',
  refund: 'Refund / return',
  quality: 'Product quality',
  'customer-care': 'Customer care',
  fraud: 'Fake or wrong product',
  pricing: 'Pricing',
  other: 'Other',
};

export const LISTING_TYPES = {
  Product: 'Product',
  Seller: 'Seller',
  Service: 'Service company',
};

export const day = (iso) => (iso ? String(iso).slice(0, 10) : '');
