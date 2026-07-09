// Ürün kataloğu çağrıları (Inventory servisi, Gateway üzerinden). docs/03 M2.
//   GET    /api/inventory/products?page&pageSize&onlyActive
//   GET    /api/inventory/products/{id}
//   POST   /api/inventory/products         → { id }
//   PUT    /api/inventory/products/{id}    → 200
//   DELETE /api/inventory/products/{id}    → 200 (soft-delete)

import { apiClient } from '@/lib/api-client';
import type { Product } from '@/shared/entities';

export interface ProductInput {
  name: string;
  category: string | null;
  defaultUnit: number;
}

export function getProduct(id: string): Promise<Product> {
  return apiClient.get<Product>(`/api/inventory/products/${id}`);
}

export function createProduct(input: ProductInput): Promise<{ id: string }> {
  return apiClient.post<{ id: string }>('/api/inventory/products', input);
}

export function updateProduct(id: string, input: ProductInput): Promise<unknown> {
  return apiClient.put(`/api/inventory/products/${id}`, input);
}

export function deactivateProduct(id: string): Promise<unknown> {
  return apiClient.delete(`/api/inventory/products/${id}`);
}
