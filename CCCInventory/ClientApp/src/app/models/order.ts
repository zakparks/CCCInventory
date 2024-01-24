import { Time } from "@angular/common";
import { Cake } from "./cake";
import { Cookie } from "./cookie";
import { Cupcake } from "./cupcake";
import { Pupcake } from "./pupcake";

export class Order {
  OrderNumber: number | undefined;
  OrderDate: Date | undefined;
  PickupTime: Time | undefined;
  DeliveryLocation?: string;
  CustName: string | undefined;
  CustEmail: string | undefined;
  CustPhone: string | undefined;
  FormFiles?: Array<File>;
  Details: string | undefined;
  PickupOrDelivery: boolean | undefined;
  SecondaryName?: string;
  SecondaryPhone?: string;
  InitialContact?: string;
  ContractSent?: boolean | false;
  DayOfTextSent?: boolean | false;
  ConfirmationTextSent?: boolean | false;
  Cakes?: Array<Cake>;
  Cupcakes?: Array<Cupcake>;
  Pupcakes?: Array<Pupcake>;
  Cookies?: Array<Cookie>;
  TotalCost?: number;
  DepositAmount?: number;
  DepositPaymentMethod?: string;
  DepositDateTime?: Date;
  FinalPaymentMethod?: string;
  FinalPaymentDateTime?: Date;
  DateOrderPlaced: Date | undefined;
  PaidInFull?: boolean | undefined;
}
