using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class BookTests : IDisposable
    {
        private RestClient client;
        private string token;
        private Random random;
        private string title;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");

            random = new Random();
        }

        [Test, Order(1)]
        public void Test_GetAllBooks()
        {
            // Arrange
            var request = new RestRequest("/book", Method.Get);

            // Act
            var response = client.Execute(request);

            // Asserts
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to get the books.");
                Assert.That(response.Content, Is.Not.Null.Or.Empty,
                    "The response content for geting books is empty.");

                // Data Structure Assertions
                var books = JArray.Parse(response.Content);

                Assert.That(books.Type, Is.EqualTo(JTokenType.Array),
                    "Expected response createdRecepie is not a JSON array");
                Assert.That(books.Count, Is.GreaterThan(0),
                    "Books count is below 1");

                // Book Fields Assertions (for each book)
                foreach (var book in books)
                {
                    Assert.That(book["title"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Title is not as expected");
                    Assert.That(book["author"], Is.Not.Null.Or.Empty,
                        "Author is not as expected");
                    Assert.That(book["description"], Is.Not.Null.Or.Empty,
                        "Description is not as expected");
                    Assert.That(book["price"], Is.Not.Null.Or.Empty,
                        "Price is not as expected");
                    Assert.That(book["pages"], Is.Not.Null.Or.Empty,
                        "Pages is not as expected");
                    Assert.That(book["category"], Is.Not.Null.Or.Empty,
                        "Category is not as expected");
                }
            });
        }

        [Test, Order(2)]
        public void Test_GetBookByTitle()
        {
            // Arrange
            var getRequest = new RestRequest("book", Method.Get);

            // Act
            var getResponse = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to retrieve books by title.");
                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "The response content for retrieving books by title is empty.");

                // Data Structure Assertions
                var books = JArray.Parse(getResponse.Content);

                var book = books.FirstOrDefault
                (b => b["title"]?.ToString() == "The Great Gatsby");

                Assert.That(book["title"]?.ToString(), Is.EqualTo("The Great Gatsby"),
                    "Title does not contain correct value");

                Assert.That(book["author"]?.ToString(),
                    Is.EqualTo("F. Scott Fitzgerald"),
                    "Author does not contain correct value");
            });
        }

        [Test, Order(3)]
        public void Test_AddBook()
        {
            // Arrange
            // Get all categories
            var getCategoriesRequest = new RestRequest("/category", Method.Get);
            var getCategoriesResponse = client.Execute(getCategoriesRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getCategoriesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to get book categories.");
                Assert.That(getCategoriesResponse.Content, Is.Not.Null.Or.Empty,
                    "The response content for get book categories is empty.");
            });

            var categories = JArray.Parse(getCategoriesResponse.Content);

            // Extract the first category id
            var categoryId = categories.First()["_id"]?.ToString();

            // Create a request for creating a book
            var createBookRequest = new RestRequest("/book", Method.Post);
            createBookRequest.AddHeader("Authorization", $"Bearer {token}");

            var title = $"bookTitle_{random.Next(999, 9999)}";
            var author = "Random Author";
            var description = "Random Description";
            var price = 20;
            var pages = 350;
            var category = categoryId;


            createBookRequest.AddJsonBody(new
            {
                title = title,
                author,
                description,
                price,
                pages,
                category = categoryId
            });

            // Act
            var addResponse = client.Execute(createBookRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(addResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to add book.");
                Assert.That(addResponse.Content, Is.Not.Empty,
                    "The response content for adding book is empty.");
            });

            // Get the details of the Book
            var createdBook = JObject.Parse(addResponse.Content);
            var createdBookId = createdBook["_id"]?.ToString();

            // Get request for getting by id
            var getByIdRequest = new RestRequest($"/book/{createdBookId}", Method.Get);
            var getByIdResponse = client.Execute(getByIdRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to get book by id.");
                Assert.That(getByIdResponse.Content, Is.Not.Empty,
                    "The response content for get book by id is empty.");

                var createdBook = JObject.Parse(getByIdResponse.Content);

                // Book Fields Assertions
                Assert.That(createdBook["title"]?.ToString(), Is.EqualTo(title),
                    "Title does not match the input");
                Assert.That(createdBook["author"]?.ToString(), Is.EqualTo(author),
                    "Author does not match the input");
                Assert.That(createdBook["description"]?.ToString(), Is.EqualTo(description),
                    "Description does not match the input");
                Assert.That(createdBook["price"]?.Value<int>(), Is.EqualTo(price),
                    "Price does not match the input");
                Assert.That(createdBook["pages"]?.Value<int>(), Is.EqualTo(pages),
                    "Pages servings does not match the input");
                Assert.That(createdBook["category"]?["_id"]?.ToString(), Is.EqualTo(categoryId),
                    "Category does not match the input");
            });
        }

        [Test, Order(4)]
        public void Test_UpdateBook()
        {
            // Arrange
            // Get by title
            var getRequest = new RestRequest("/book", Method.Get);

            var getResponse = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to retrieve books.");
                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "The response content for retrieving books is empty.");
            });

            var books = JArray.Parse(getResponse.Content);
            var bookToUpdate = books.FirstOrDefault
                (b => b["title"]?.ToString() == "The Catcher in the Rye");

            Assert.That(bookToUpdate, Is.Not.Null.Or.Empty,
                "The book 'The Catcher in the Rye' is not found in the retrieved books.");
            Assert.That(bookToUpdate["title"].ToString(), Is.EqualTo("The Catcher in the Rye"),
                "The 'title' of the book to update does not match 'The Catcher in the Rye'.");

            // Get the id of the book
            var bookId = bookToUpdate["_id"]?.ToString();

            // Create update request
            var updateRequest = new RestRequest($"/book/{bookId}", Method.Put);

            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddUrlSegment("id", bookId);
            updateRequest.AddJsonBody(new
            {
                title = "Updated Book Title",
                author = "Updated Author"
            });

            // Act
            var updateResponse = client.Execute(updateRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to update the book.");
                Assert.That(updateResponse.Content, Is.Not.Null.Or.Empty,
                    "The response content for updating the book is empty.");

                var updatedBook = JObject.Parse(updateResponse.Content);

                Assert.That(updatedBook["title"]?.ToString(), Is.EqualTo("Updated Book Title"),
                    "The 'title' does not match the updated value");
                Assert.That(updatedBook["author"]?.ToString(), Is.EqualTo("Updated Author"),
                    "The 'author' does not match the updated value");
            });
        }

        [Test, Order(5)]
        public void Test_DeleteBook()
        {
            // Arrange
            // Get all books
            var getRequest = new RestRequest("/book", Method.Get);

            var getResponse = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to retrieve books.");
                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "The response content for retrieving books is empty.");
            });

            var books = JArray.Parse(getResponse.Content);
            var bookToDelete = books.FirstOrDefault
                (b => b["title"]?.ToString() == "To Kill a Mockingbird");

            Assert.That(bookToDelete, Is.Not.Null.Or.Empty,
                "The book 'To Kill a Mockingbird' is not found in the retrieved books.");
            Assert.That(bookToDelete["title"].ToString(), Is.EqualTo("To Kill a Mockingbird"),
                "The 'title' of the book to delete does not match 'To Kill a Mockingbird'.");

            // Get the id of the book
            var bookId = bookToDelete["_id"]?.ToString();

            // Create delete request
            var deleteRequest = new RestRequest($"/book/{bookId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");
            deleteRequest.AddUrlSegment("id", bookId);

            // Act
            var deleteResponse = client.Execute(deleteRequest);

            // Assert
            // Post-Deletion Verification
            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Failed to delete the book.");

                // Get request by id
                var verifyDeleteRequest = new RestRequest($"/book/{bookId}", Method.Get);
                verifyDeleteRequest.AddUrlSegment("id", bookId);

                var verifyDeleteResponse = client.Execute(verifyDeleteRequest);

                Assert.That(verifyDeleteResponse.Content, Is.EqualTo("null"),
                    "The deleted book is still accessible.");
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
