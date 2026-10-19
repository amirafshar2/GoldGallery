namespace Gold.Domain.Entities;

public enum UserRole { Customer = 1, Wholesale = 2, Staff = 3, Admin = 4 }

public enum Audience { Women = 1, Men = 2, Kids = 3, Unisex = 4 }

public enum GoldColor { Yellow = 1, White = 2, Rose = 3 }

public enum PieceStatus { Available = 1, Reserved = 2, Sold = 3 }

public enum OrderStatus { Draft = 1, Quoted = 2, AwaitingPayment = 3, Paid = 4, Packed = 5, Shipped = 6, PickedUp = 7, Delivered = 8, Cancelled = 9, Refunded = 10 }

public enum PaymentStatus { Pending = 1, Succeeded = 2, Failed = 3 }

public enum LedgerDirection { In = 1, Out = 2 }
