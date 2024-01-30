import { Time } from "@angular/common";
import { Cake } from "./cake";
import { Cookie } from "./cookie";
import { Cupcake } from "./cupcake";
import { Pupcake } from "./pupcake";

export class Order {
  orderNumber: number | undefined;
  orderDate: Date | undefined;
  pickupTime: Time | undefined;
  deliveryLocation?: string;
  custName: string | undefined;
  custEmail: string | undefined;
  custPhone: string | undefined;
  formFiles?: Array<File>;
  details: string | undefined;
  pickupOrDelivery: boolean | undefined;
  secondaryName?: string;
  secondaryPhone?: string;
  initialContact?: string;
  contractSent?: boolean | false;
  dayOfTextSent?: boolean | false;
  confirmationTextSent?: boolean | false;
  cakes?: Array<Cake>;
  cupcakes?: Array<Cupcake>;
  pupcakes?: Array<Pupcake>;
  cookies?: Array<Cookie>;
  totalCost?: number;
  depositAmount?: number;
  depositPaymentMethod?: string;
  depositDateTime?: Date;
  finalPaymentMethod?: string;
  finalPaymentDateTime?: Date;
  dateOrderPlaced: Date | undefined;
  paidInFull?: boolean | undefined;
}
