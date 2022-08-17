using System;
using System.Threading.Tasks;
using Xunit;

namespace Zucchetti.Tests
{
    public class ZucchettiTests
    {
        private const string baseURL = "";
        private const string userName = "";
        private const string password = "";

        [Fact]
        public async Task ShouldNotLoginCorrectly()
        {
            var zClient = new ZucchettiClient(baseURL, "user", "password");

            await Assert.ThrowsAsync<Exception>(async Task () => await zClient.LoginAsync());
        }

        [Fact]
        public async Task ShouldLoginCorrectly()
        {
            var zClient = new ZucchettiClient(baseURL, userName, password);

            await zClient.LoginAsync();
        }

        [Fact]
        public async Task ShouldRetrieveStamps()
        {
            var zClient = new ZucchettiClient(baseURL, userName, password);
            await zClient.LoginAsync();

            var stamps = await zClient.RetrieveStampsAsync(DateOnly.Parse("2022/08/16"));

            Assert.NotEmpty(stamps);
        }

        [Fact]
        public async Task ShouldBeAbleToRetrieveMCID()
        {
            var zClient = new ZucchettiClient(baseURL, userName, password);
            await zClient.LoginAsync();

            var mCID = await zClient.RetrieveMCIDAsync();

            Assert.False(string.IsNullOrEmpty(mCID));
        }

        [Fact(Skip = "⚠️ This Test alters data on the server")]
        public async Task ShouldClockInAndOut()
        {
            var zClient = new ZucchettiClient(baseURL, userName, password);
            await zClient.LoginAsync();

            await zClient.ClockInAsync();
            await zClient.ClockOutAsync();
        }
    }
}
