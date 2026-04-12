export class Customer {
  customerId?: number;
  firstName: string | undefined;
  lastName: string | undefined;
  email?: string;
  phone?: string;
  orders?: any[];
  orderCount?: number;
}

export class CustomerSearchResult {
  customerId: number | undefined;
  firstName: string | undefined;
  lastName: string | undefined;
  email?: string;
  phone?: string;
}
