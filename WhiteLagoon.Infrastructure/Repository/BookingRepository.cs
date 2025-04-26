using WhiteLagoon.Application.Common.Interfaces;
using WhiteLagoon.Application.Common.Utility;
using WhiteLagoon.Domain.Entities;
using WhiteLagoon.Infrastructure.Data;
using WhiteLagoon.Infrastructure.Repository;

namespace WhiteLagoon.Infrastructure.Repository;

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    private readonly ApplicationDbContext _db;
    public BookingRepository(ApplicationDbContext db) : base(db)
    {
        _db = db ;
    }

    public void Update(Booking entity)
    {
        _db.Bookings.Update(entity);
    }
    public void UpdateStatus(int bookingId, string bookingStatus, int villaNumber = 0)
    {
        var bookingFormDb = _db.Bookings.FirstOrDefault(b => b.Id == bookingId);
        if (bookingFormDb != null)
        {
            bookingFormDb.Status = bookingStatus;
            if (bookingStatus == SD.StatusCheckedIn)
            {
                bookingFormDb.VillaNumber = villaNumber;
                bookingFormDb.ActualCheckInDate = DateTime.Now;
            }
            if (bookingStatus == SD.StatusCompleted)
            {
                bookingFormDb.ActualCheckOutDate = DateTime.Now;
            }
        }
    }
    public void UpdateStripePaymentId(int bookingId, string sessionID, string paymentIntentId)
    {
        var bookingFormDb = _db.Bookings.FirstOrDefault(b => b.Id == bookingId);
        if (bookingFormDb != null)
        {
            if(!string.IsNullOrEmpty(sessionID))
            {
                bookingFormDb.StripeSessionId = sessionID;
            }
            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                bookingFormDb.StripePaymentIntentId = paymentIntentId;
                bookingFormDb.PaymentDate = DateTime.Now;
                bookingFormDb.IsPaymentSuccessful = true;
            }
        }
    }
}
