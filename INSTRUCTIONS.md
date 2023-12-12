
### Your goals

1. Build a backend service which:

   - Exposes an HTTP endpoint, `/upload`

     - e.g. `curl -F 'file=@"invoices/HubdocInvoice1.pdf"' -F 'email=user@domain.com' localhost:3000/upload`
     - Accepts a .pdf document found in `./invoices/` folder and a user email in the body of the request
        - Note: If you are unable to transform pdf to text, your api is allowed to accept *.txt files instead. Transformed text versions located in `./invoices-text/` folder.
     - Attempts to extract the following data from the document
       - _Vendor_ (e.g. Starbucks, Home Depot, McDonalds)
       - _Invoice Date_
       - _Total Amount_ (a postitive or negative value with at most 2 decimal places)
       - _Total Amount Due_ (a postitive or negative value with at most 2 decimal places)
       - _Currency_ (a three character currency code; e.g. CAD, GBP)
       - _Tax_ (a positive or negative value with at most 2 decimal places)
     - Responds with a JSON payload containing an assigned document id:

     ```javascript
     {
       id: <someUniqueId>
     }
     ```

   - Exposes an HTTP endpoint, `/document/:id`

     - `curl -XGET http://localhost:3000/document/:id`
     - Respond with the following payload:

       ```javascript
       {
         uploadedBy : '<userEmailAddress>',
         uploadTimestamp : '<timestamp>',
         filesize: '<filesize>',
         vendorName: '<vendorName>',
         invoiceDate: '<invoiceDate>',
         totalAmount: '<totalAmount>',
         totalAmountDue: '<totalAmountDue>',
         currency: '<currency>',
         taxAmount: '<taxAmount>',
         processingStatus: '<status>', // processingStatus should reflect the current state of document processing after submission. This is open to your interpretation.
       }
       ```

     - If you are unable to successfully extract a given field, you can set the response value to `null` or `undefined`.
     - The only fields that must always have a value are `uploadedBy` and `uploadTimestamp`

### Helpful tool

We recommend (not required) that you use `pdftotext` in order to extract text from pdf documents. See [pdftotext](PDFTOTEXT_INSTRUCTIONS.md) for instructions

### Whats included

- `invoices` folder that contains a set of Hubdoc invoices. Your service should correctly extract the expected fields from all supplied invoices.
- `invoices-text` folder that shows what you can expect from the pdftotext application

## We expect the following from you

- Working code that accomplishes the tasks outlined above.
- Corresponding tests (unit and integration tests are both acceptable).
- Document how to run your code and if your code is OS specific.
- Document any work that needs further clarification.
- Document any assumptions made and decisions made based on time with the logic used and how things would be handled if you had more time

## We do NOT expect you to worry about the following

- Deploying the service or user authentication or any other form of endpoint security
  - _For senior candidates we may ask how you would accomplish this, but we don't expect to see code here_
- Virus checking or document format validation (i.e. you can assume we will send you valid pdfs)
