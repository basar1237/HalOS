// Sayfalanmış liste yanıtı — backend PagedResult<T> ile birebir (camelCase JSON).
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
