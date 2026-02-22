export interface OrderAttachment {
  id: number;
  orderNumber: number;
  fileName: string;
  storedFileName: string;
  contentType: string;
  uploadedAt: Date;
}
