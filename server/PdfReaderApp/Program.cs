using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using IronPdf;

var builder = WebApplication.CreateBuilder(args);

// PDF upload will fail on parse due to free version not converting whole pdf
IronPdf.License.LicenseKey = "";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

Dictionary < string, string > recordDictionary = new Dictionary < string, string > ();

app.MapGet("/document/{id}", ([FromRoute] string id) => {
    if (recordDictionary.TryGetValue(id, out string recordData)) {
      return Results.Ok(recordData);
    } else {
      return Results.NotFound($"Error, no record found with given ID: {id}");
    }
  })
  .WithName("GetPdfSummary")
  .WithOpenApi();

app.MapPost("/upload",
    async (HttpRequest request) => {
      if (!request.Form.Files.Any()) {
        return Results.BadRequest("Missing file to upload");
      }
      var userEmail = request.Form["email"];
      if (!IsValidEmail(userEmail)) {
        return Results.BadRequest("Missing or invalid email");
      }
      var uploadedFile = request.Form.Files[0];
      string fileSize = uploadedFile.Length.ToString() + " bytes";
      var txtData = await GetConvertedTxtFile(uploadedFile);
      if (txtData == "error") {
        //hacky, will refactor to return error type if given more time
        return Results.BadRequest("Invalid file type");
      } else if (txtData.EndsWith("https://ironpdf.com/licensing/")) {
        return Results.BadRequest("PDF plugin require paid version, please try uploading .txt instead. IronPdf plugin converted this as preview: " + txtData);
      }
      string pdfRecord = GeneratePdfJsonRecord(userEmail, fileSize, txtData);
      Guid newEntryId = Guid.NewGuid();
      recordDictionary[newEntryId.ToString()] = pdfRecord;
      return Results.Ok(ConvertToJson(new UploadRecord {
        Id = newEntryId.ToString()
      }));
    }).WithName("UploadPdf")
  .WithOpenApi();

app.Run();

// Todo: refactor helper functions below if they are ever to be used by other files

static bool IsValidEmail(string email) {
  if (email == null) {
    return false;
  }
  string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
  return Regex.IsMatch(email, emailPattern);
}

async static Task < string > GetConvertedTxtFile(IFormFile uploadedFile) {
  string fileExtension = Path.GetExtension(uploadedFile.FileName);
  if (fileExtension == ".txt") {
    using(var streamReader = new StreamReader(uploadedFile.OpenReadStream())) {
      string txtData = await streamReader.ReadToEndAsync();
      return txtData;
    }
  }
  if (fileExtension == ".pdf") {
    var filePath = Path.GetTempFileName();
    using(var stream = System.IO.File.Create(filePath)) {
      await uploadedFile.CopyToAsync(stream);
    }
    using PdfDocument PDF = PdfDocument.FromFile(filePath, "");
    string txtData = PDF.ExtractAllText();
    Console.WriteLine(txtData);
    return txtData;
  } else {
    return "invalid file type";
  }
}

static string GeneratePdfJsonRecord(string email, string fileSize, string rawText) {
  string
  uploadedBy = email,
    uploadTimestamp = DateTimeOffset.Now.ToString(),
    filesize = fileSize,
    vendorName = null,
    invoiceDate = null,
    totalAmount = null,
    totalAmountDue = null,
    currency = null,
    taxAmount = null,
    processingStatus = null;

  string[] lines = rawText.Split(new [] {
    "\r\n",
    "\r",
    "\n"
  }, StringSplitOptions.None);

  // Knowing where the table should start prevents reading user data as table values (ie company name = Total Due)
  int invoiceTableStartPoint = lines.ToList().FindIndex(s => s.StartsWith("This email confirms your", StringComparison.Ordinal));

  try {
    for (int i = 0; i < invoiceTableStartPoint; i++) {
      string line = lines[i];
      if (line.Trim().StartsWith("Invoice") && GetWords(line).Length == 5) {
        // The with date info always have 5 words starting with Invoice, we want the last 3 for date
        string timeString = string.Join(" ", Enumerable.Reverse(GetWords(line)).Take(3).Reverse().ToList());
        invoiceDate = DateTime.ParseExact(timeString, "MMMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString();
        vendorName = lines[i + 2].Trim();
        break;
      }
    }

    for (int i = invoiceTableStartPoint; i < lines.Length; i++) {
      string line = lines[i];
      if (line.Trim().StartsWith("Tax") && GetWords(line).Length == 3) {
        taxAmount = Regex.Match(GetWords(line)[2], @"-?\d+(\.\d+)?").Value.ToString();
      }
      if (line.Trim().StartsWith("Total Due")) {
        totalAmountDue = Regex.Match(GetWords(line)[2], @"-?\d+(\.\d+)?").Value.ToString();
        currency = lines[i + 2].Trim();
      } else if (line.Trim().StartsWith("Total")) {
        totalAmount = Regex.Match(GetWords(line)[1], @"-?\d+(\.\d+)?").Value.ToString();
      }
    }
    processingStatus = "Automatically verified";
  } catch {
    //TODO: log exceptions
    processingStatus = "Awaiting manual review";
  }

  InvoiceData invoiceData = new InvoiceData {
    UploadedBy = uploadedBy,
      UploadTimestamp = uploadTimestamp,
      Filesize = filesize,
      VendorName = vendorName,
      InvoiceDate = invoiceDate,
      TotalAmount = totalAmount,
      TotalAmountDue = totalAmountDue,
      Currency = currency,
      TaxAmount = taxAmount,
      ProcessingStatus = processingStatus
  };

  string json = ConvertToJson(invoiceData);
  return json;
}

static string[] GetWords(string input) {
  // Split the input string into words using whitespace as the delimiter
  return input.Split(new [] {
    ' ',
    '\t',
    '\n',
    '\r'
  }, StringSplitOptions.RemoveEmptyEntries);
}

static string ConvertToJson(object obj) {
  string jsonString = JsonSerializer.Serialize(obj);
  return jsonString;
}