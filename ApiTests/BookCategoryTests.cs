using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;
using System.Xml.Linq;

namespace ApiTests
{
    [TestFixture]
    public class BookCategoryTests : IDisposable
    {
        private RestClient client;
        private string token;
        private Random random;
        private string title;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            random = new Random();
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
        }

        [Test]
        public void Test_BookCategoryLifecycle()
        {
            // Step 1: Create a new category
            var title = $"categoryTitle_{random.Next(999, 9999)}";
            var createRequest = new RestRequest("/category", Method.Post);
            createRequest.AddHeader("Authorization", $"Bearer {token}");
            createRequest.AddJsonBody(new
            {
                title
            });

            // Act
            var createResponse = client.Execute(createRequest);

            // Assert
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The response does not contain the correct status code OK (200)");

            var createdCategory = JObject.Parse(createResponse.Content);
            Assert.That(createdCategory["_id"]?.ToString(), Is.Not.Null.Or.Empty,
                "Category ID is not as expected");

            // Step 2: Retrieve all book categories and verify the newly created category is present

            var getAllCategoriesRequest = new RestRequest("/category", Method.Get);
            var getAllCategoriesResponse = client.Execute(getAllCategoriesRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getAllCategoriesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getAllCategoriesResponse.Content, Is.Not.Null.Or.Empty,
                    "The getAllCategoriesResponse content is empty");

                var categories = JArray.Parse(getAllCategoriesResponse.Content);
                Assert.That(categories?.Type, Is.EqualTo(JTokenType.Array),
                    "Expected response content is not a JSON array");
                Assert.That(categories.Count, Is.GreaterThan(0),
                    "Expected at least one category in the response");
            });

            var categoryId = createdCategory["_id"]?.ToString();

            var getByIdRequest = new RestRequest($"/category/{categoryId}", Method.Get);
            var getByIdResponse = client.Execute(getByIdRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getByIdResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is empty");

                var categoryById = JObject.Parse(getByIdResponse.Content);
                Assert.That(categoryById["_id"]?.ToString(), Is.EqualTo(categoryId),
                    "Expected the category ID to match");
                Assert.That(categoryById["title"]?.ToString(), Is.EqualTo(title),
                    "Expected the category title to match");
            });

            // Step 3: Update the category title

            var editRequest = new RestRequest($"/category/{categoryId}", Method.Put);
            var udpatedCategoryTitle = title + "_updated";
            editRequest.AddHeader("Authorization", $"Bearer {token}");
            editRequest.AddJsonBody(new
            {
                title = udpatedCategoryTitle
            });

            var editResponse = client.Execute(editRequest);
            Assert.That(editResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The response does not contain the correct status code OK (200)");

            // Step 4: Verify that the category details have been updated

            var getUpdatedCategoryRequest = new RestRequest($"/category/{categoryId}", Method.Get);
            var getUpdatedCategoryResponse = client.Execute(getUpdatedCategoryRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getUpdatedCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getUpdatedCategoryResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is empty");

                var updatedCategory = JObject.Parse(getUpdatedCategoryResponse.Content);
                Assert.That(updatedCategory["title"]?.ToString(), Is.EqualTo(udpatedCategoryTitle),
                    "The updated category title does not match");
            });

            // Step 5: Delete the category and validate it's no longer accessible

            var deleteRequest = new RestRequest($"category/{categoryId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest);
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The response does not contain the correct status code OK (200)");

            // Step 6: Verify that the deleted category cannot be found

            var getDeletedCategoryRequest = new RestRequest($"/category/{categoryId}", Method.Get);
            var getDeletedCategoryResponse = client.Execute(getDeletedCategoryRequest);

            Assert.That(getDeletedCategoryResponse.Content, Is.Empty.Or.EqualTo("null"),
                "Deleted category should not be found");

        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
