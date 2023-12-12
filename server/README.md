# Kong's takehome PDF parser App

## Summary

I wrote this App according to the instructions. My project runs on `localhost:3000` and has exposes `localhost:3000/upload` as POST and `localhost:3000/document/:id` as GET

To run the program:
```bash
cd PdfReaderApp

dotnet run
```

The setup is tested for Windows.

## Sample Screenshots
I used Postman to test the exposed HTTP endpoints

Here are screenshots of result from [POST](https://snipboard.io/xNsgTh.jpg) and [GET](https://snipboard.io/ZsVOmK.jpg)

## Assumptions

For any record where pdf/txt is updated, we are generating a record. The `processingStatus` field indicates whether all of the file was successfully read. If not, we will include on the final payload `processingStatus: "Awaiting manual review"`

Also, I assume that in the case that we try to upload an invalid file, upload no file, or provide null or invalid email, we should respond with a status 400 Bad Request response.

I also assumed that I should not spend more than 4 hours on this and thus did not spend extra time to write tests.

## Approach

First, I started by uploading the txt file to be read from filepath and wrote the function to get extract all key information from the file.

Second, I worked on setting up the POST endpoint to allow file from request to replace file read from filepath as the source and tested by verifying that the same result as step 1 returned to me.

Afterwards, I went down quite a rabbit hole trying to get PDF conversion to .txt working on C#. I some research and ended using IronPDF but found that it was only partially working due to requiring a license in order to convert the entire PDF

The next task I did was to add the GUID generation and adding parsed results to a dictionary. The GET functionalities were pretty straightforward after the establishment of this dictionary.

At this stage, I was running low on time, I then wrote out the plans I have for testing in the test file and added basic error cases to my HTTP response.

The last step I did was about half an hour of manual testing and setting up the project again on a fresh environment .

I have timeboxed this project to take 4 hours. I think a lot more can be done to improve the robustness of this project

## Next Step

If I have more time, I will first set up all the types of tests I have listed out.

Next, I think it will be very useful to set up a DB so that data is not lost every time we stop the server. I would use PostgreSQL for its easy setup. The only table needed at first is the dictionary of all records.

Next on the priority list would be the ability to edit a record found using ID and manually update details.

Then we could think about adding auth and permission, starting with giving Superuser with all access and regular user the permission to only view/edit the entries they made.