using System.Drawing;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.BillingPortal;
using Stripe.Checkout;
using Stripe.FinancialConnections;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using WhiteLagoon.Application.Common.Interfaces;
using WhiteLagoon.Application.Common.Utility;
using WhiteLagoon.Domain.Entities;


namespace WhiteLagoon.Web.Controllers;

public class BookingController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public BookingController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }
    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult FinalizeBooking(int villaId, DateOnly CheckInDate, int nights)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        ApplicationUser? user = _unitOfWork.User.Get(u => u.Id == userId);
        Villa? villa = _unitOfWork.Villa.Get(u => u.Id == villaId, includeProperties: "VillaAmenity");
        
        if (user == null || villa == null)
        {
            return NotFound();
        }
        
        Booking booking = new()
        {
            VillaId = villaId,
            Villa = villa,
            CheckInDate = CheckInDate,
            Nights = nights,
            CheckOutDate = CheckInDate.AddDays(nights),
            UserId = userId,
            Phone = user.PhoneNumber,
            Email = user.Email,
            Name = user.Name,
            User = user,
            VillaNumbers = new List<VillaNumber>()
        };
        booking.TotalCost = booking.Villa.Price * nights;
        return View(booking);
    }
    

    [Authorize]
    [HttpPost]
    public IActionResult FinalizeBooking(Booking booking)
    {
        Villa? villa = _unitOfWork.Villa.Get(u => u.Id == booking.VillaId);
        if (villa == null)
        {
            return NotFound();
        }
        
        booking.TotalCost = villa.Price * booking.Nights;
        booking.Status = SD.StatusPending;
        booking.BookingDate = DateTime.Now;
        var VillaNumbersList = _unitOfWork.VillaNumber.GetAll().ToList();
        var bookedVillas = _unitOfWork.Booking.GetAll(u => u.Status == SD.StatusApproved || u.Status == SD.StatusCheckedIn).ToList();
        int roomAvailable = SD.VillaRoomsAvailable_Count(villa.Id, VillaNumbersList, booking.CheckInDate, booking.Nights, bookedVillas);

        if (roomAvailable == 0)
        {
            TempData["error"] = "Room has been sold out!";
            //no rooms available
            return RedirectToAction(nameof(FinalizeBooking), new
            {
                villaId = booking.VillaId,
                checkInDate = booking.CheckInDate,
                nights = booking.Nights
            });
        }

        _unitOfWork.Booking.Add(booking);
        _unitOfWork.Save(); 

        var domain = Request.Scheme + "://" + Request.Host.Value + "/";
        var options = new Stripe.Checkout.SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
            SuccessUrl = domain + "Booking/BookingConfirmation?BookingId=" + booking.Id,
            CancelUrl = domain + "Booking/FinalizeBooking?VillaId=" + booking.VillaId + "&CheckInDate=" + booking.CheckInDate + "&nights=" + booking.Nights,
        };

        options.LineItems.Add(new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = (long)(booking.TotalCost * 100),
                Currency = "usd",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = villa.Name,
                    // Images = new List<string> { domain + villa.ImageUrl },
                },
            },
            Quantity = 1,
        });

        var service = new Stripe.Checkout.SessionService();
        Stripe.Checkout.Session session = service.Create(options);

        _unitOfWork.Booking.UpdateStripePaymentId(booking.Id, session.Id, session.PaymentIntentId);  
        _unitOfWork.Save(); 
        Response.Headers["Location"] = session.Url;
        return new StatusCodeResult(303);
    }

    [Authorize]
    public IActionResult BookingConfirmation(int bookingId)
    {
        Booking? bookingFormDb = _unitOfWork.Booking.Get(u => u.Id == bookingId, includeProperties: "User,Villa");
        if (bookingFormDb == null)
        {
            return NotFound();
        }
        
        if(bookingFormDb.Status == SD.StatusPending)
        {
            // this is pending order , we need to confirm the payment was successful
            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = service.Get(bookingFormDb.StripeSessionId);
            if (session.PaymentStatus == "paid")
            {
                _unitOfWork.Booking.UpdateStatus(bookingFormDb.Id, SD.StatusApproved, 0);
                _unitOfWork.Booking.UpdateStripePaymentId(bookingFormDb.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
            }
        }
        return View(bookingId);
    }

    [Authorize]
    public IActionResult BookingDetails(int bookingId)
    {
        Booking? bookingFromDb = _unitOfWork.Booking.Get(u => u.Id == bookingId, includeProperties: "User,Villa");
        if (bookingFromDb == null)
        {
            return NotFound();
        }

        if(bookingFromDb.VillaNumber == 0 && bookingFromDb.Status == SD.StatusApproved)
        {
            var availableVillaNumber = AssignAvailableVillaNumberByVilla(bookingFromDb.VillaId);
            bookingFromDb.VillaNumbers = _unitOfWork.VillaNumber.GetAll(u => u.VillaId == bookingFromDb.VillaId 
                && availableVillaNumber.Any(x => x == u.Villa_Number)).ToList();
        }
        return View(bookingFromDb);
    }

    public List<int> AssignAvailableVillaNumberByVilla(int villaId)
    {
        List<int> assignAvailableVillaNumber = new();
        var villaNumbers = _unitOfWork.Booking.GetAll(u => u.VillaId == villaId);
        var checkedInVilla = _unitOfWork.Booking.GetAll(u => u.VillaId == villaId && u.Status == SD.StatusCheckedIn).
            Select(u => u.VillaNumber);
        foreach (var villaNumber in villaNumbers)
        {
            if (!checkedInVilla.Contains(villaNumber.VillaNumber))
            {
                assignAvailableVillaNumber.Add(villaNumber.VillaNumber);
            }
        }
        return assignAvailableVillaNumber;
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin)]
    public IActionResult CheckOut(Booking booking)
    {
        _unitOfWork.Booking.UpdateStatus(booking.Id, SD.StatusCompleted, booking.VillaNumber);
        _unitOfWork.Save();
        TempData["success"] = "Booking Completed Successfully";
        return RedirectToAction(nameof(BookingDetails), new { bookingId = booking.Id });
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin)]
    public IActionResult CancelBooking(Booking booking)
    {
        _unitOfWork.Booking.UpdateStatus(booking.Id, SD.StatusCancelled, 0);
        _unitOfWork.Save();
        TempData["success"] = "Booking Cancelled Successfully";
        return RedirectToAction(nameof(BookingDetails), new { bookingId = booking.Id });
    }

    [HttpPost]
    // [Authorize]
    public IActionResult GenerateInvoice(int id, string downloadType)
    {
        string basePath = _webHostEnvironment.WebRootPath;
        WordDocument document = new WordDocument();

        // Load template
        string path = basePath + @"/exports/BookingDetails.docx";
        using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        document.Open(fileStream, FormatType.Docx);

        // Update template
        Booking? bookingFromDb = _unitOfWork.Booking.Get(u => u.Id == id, includeProperties: "User,Villa");
        if (bookingFromDb == null)
        {
            return NotFound();
        }
        
        TextSelection textSelection = document.Find("xx_customer_name", false, true);
        WTextRange textRange = textSelection.GetAsOneRange();
        textRange.Text = bookingFromDb.Name;

        textSelection = document.Find("xx_customer_phone", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = bookingFromDb.Phone ?? string.Empty;

        textSelection = document.Find("xx_customer_email", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = bookingFromDb.Email;

        textSelection = document.Find("xx_booking_number", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = "Booking ID: " + bookingFromDb.Id;
        textSelection = document.Find("xx_booking_date", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = "Booking Date: " + bookingFromDb.BookingDate.ToShortDateString();

        textSelection = document.Find("xx_payment_date", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = bookingFromDb.PaymentDate.ToString();
        textSelection = document.Find("xx_checkin_date", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = bookingFromDb.CheckInDate.ToString();
        textSelection = document.Find("xx_checkout_date", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = bookingFromDb.CheckOutDate.ToString();
        textSelection = document.Find("xx_booking_total", false, true);
        textRange = textSelection.GetAsOneRange();
        textRange.Text = bookingFromDb.TotalCost.ToString("C");
        
        WTable table = new(document);
        table.TableFormat.Borders.LineWidth = 1f;
        table.TableFormat.Borders.Color = Syncfusion.Drawing.Color.Black;
        table.TableFormat.Paddings.Top = 7f;
        table.TableFormat.Paddings.Bottom = 7f;
        table.TableFormat.Borders.Horizontal.LineWidth = 1f;

        int rows =bookingFromDb.VillaNumber > 0 ? 3 : 2;
        table.ResetCells(rows, 4);

        WTableRow row0 = table.Rows[0];

        row0.Cells[0].AddParagraph().AppendText("NIGHTS");
        row0.Cells[0].Width = 80;
        row0.Cells[1].AddParagraph().AppendText("VILLA");
        row0.Cells[1].Width = 220;
        row0.Cells[2].AddParagraph().AppendText("PRICE PER NIGHT");
        row0.Cells[3].AddParagraph().AppendText("TOTAL");
        row0.Cells[3].Width = 80;

        WTableRow row1 = table.Rows[1];
        
        row1.Cells[0].AddParagraph().AppendText(bookingFromDb.Nights.ToString());
        row1.Cells[0].Width = 80;
        row1.Cells[1].AddParagraph().AppendText(bookingFromDb.Villa.Name);
        row1.Cells[1].Width = 220;
        row1.Cells[2].AddParagraph().AppendText((bookingFromDb.TotalCost/bookingFromDb.Nights).ToString("C"));
        row1.Cells[3].AddParagraph().AppendText(bookingFromDb.TotalCost.ToString("C"));
        row1.Cells[3].Width = 80;

        if(bookingFromDb.VillaNumber > 0 )
        {
            WTableRow row2 = table.Rows[2];
        
            row2.Cells[0].Width = 80;
            row2.Cells[1].AddParagraph().AppendText("VILLA NUMBER - " + bookingFromDb.VillaNumber.ToString());
            row2.Cells[1].Width = 220;
            row2.Cells[3].Width = 80;
        }
        WTableStyle tableStyle = document.AddTableStyle("CustomStyle") as WTableStyle;
        tableStyle.TableProperties.RowStripe = 1;
        tableStyle.TableProperties.ColumnStripe = 2;
        tableStyle.TableProperties.Paddings.Top = 2;
        tableStyle.TableProperties.Paddings.Bottom = 1;
        tableStyle.TableProperties.Paddings.Left = 5.4f;
        tableStyle.TableProperties.Paddings.Right = 5.4f;

        ConditionalFormattingStyle firstRowStyle = tableStyle.ConditionalFormattingStyles.Add(ConditionalFormattingType.FirstRow);
        firstRowStyle.CharacterFormat.Bold = true;
        firstRowStyle.CharacterFormat.TextColor = Syncfusion.Drawing.Color.FromArgb(255, 255, 255);
        firstRowStyle.CellProperties.BackColor = Syncfusion.Drawing.Color.Black;

        table.ApplyStyle("CustomStyle");     

        TextBodyPart bodyPart = new(document);
        bodyPart.BodyItems.Add(table);
        document.Replace("<ADDTABLEHERE>", bodyPart, false, false);


        using DocIORenderer renderer = new();
        MemoryStream stream = new();
        if (downloadType == "word")
        {   
            document.Save(stream, FormatType.Docx);
            stream.Position = 0;
            return File(stream, "application/docx", "BookingDetails.docx");
        }
        else
        {
            PdfDocument pdfDocument = renderer.ConvertToPDF(document);
            pdfDocument.Save(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", "BookingDetails.pdf");
        }        
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin)]
    public IActionResult CheckIn(Booking booking)
    {
        _unitOfWork.Booking.UpdateStatus(booking.Id, SD.StatusCheckedIn, booking.VillaNumber);
        _unitOfWork.Save();
        TempData["success"] = "Booking Update Successfully";
        return RedirectToAction(nameof(BookingDetails), new { bookingId = booking.Id });
    }

    #region API CALLS
    [HttpGet]
    [Authorize]
    public IActionResult GetAll(string status)
    {
        IEnumerable<Booking> objBookings;
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (User.IsInRole(SD.Role_Admin))
        {
            objBookings = _unitOfWork.Booking.GetAll(includeProperties: "Villa,User");
        }
        else
        {
            objBookings = _unitOfWork.Booking.GetAll(u => u.UserId == userId, includeProperties: "Villa,User");
        }
        if (!string.IsNullOrEmpty(status))
        {
            objBookings = objBookings.Where(u => u.Status?.ToLower() == status.ToLower());
        }
        return Json(new { data = objBookings });
    }
    #endregion
}
