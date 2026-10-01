export interface WeddingDetails {
  id?: number;
  orderNumber?: number;

  // Event
  eventDate?: string | null;     // reception location is the order's deliveryLocation
  ceremonySameLocation?: boolean | null;
  ceremonyTime?: string | null;
  receptionTime?: string | null;

  // Contacts (Bride/Groom #1 is the order's customer)
  partner2Name?: string | null;
  partner2Phone?: string | null;
  dayOfContactTitle?: string | null;
  venueContactName?: string | null;
  venueContactPhone?: string | null;

  // Display / adornments
  cakeBoardColor?: string | null;
  cakeTopper?: boolean | null;
  hasFlowers?: boolean | null;
  flowerType?: string | null;
  flowersProvidedBy?: string | null;
  floristName?: string | null;
  floristPhone?: string | null;
  floristDeliveryTime?: string | null;

  // Delivery / pickup
  deliveryWindowEnd?: string | null;
  pickupPersonName?: string | null;
  pickupPersonPhone?: string | null;

  mainCakeDesignDescription?: string | null;   // contract "Description of Design"
  cupcakeDesignDescription?: string | null;
  totalServings?: number | null;

  // Generated contract (server-managed)
  contractDocId?: string | null;
  contractDocUrl?: string | null;
  contractDocName?: string | null;
  contractGeneratedAt?: string | null;
}
