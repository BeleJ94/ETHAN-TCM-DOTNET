namespace EthanTcm.Domain.Enums;

public enum CorrespondenceDirection { Incoming = 1, Outgoing = 2 }
public enum CorrespondencePriority { Low = 1, Normal = 2, High = 3, Critical = 4 }
public enum CorrespondenceConfidentiality { Internal = 1, Confidential = 2, Restricted = 3 }
public enum CorrespondenceChannel { PhysicalMail = 1, Email = 2, TaxPortal = 3, HandDelivery = 4, RegisteredMail = 5, Courier = 6 }
public enum CorrespondenceStatus
{
    Draft = 1, Registered = 2, Assigned = 3, InProgress = 4, AwaitingResponse = 5,
    SubmittedForValidation = 6, Validated = 7, ReadyToSend = 8, Sent = 9,
    Acknowledged = 10, Processed = 11, Closed = 12, Rejected = 13,
    Cancelled = 14, FiledWithoutAction = 15
}
public enum CorrespondenceDocumentType { Original = 1, Attachment = 2, SignedVersion = 3, DispatchProof = 4, Acknowledgement = 5, Other = 6 }
