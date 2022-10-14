using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Worker.Core.UnitTests;

[TestClass]
public class BucketDispatcherTests
{
	[TestMethod]
	public async Task BucketDispatcher_分派Command()
	{
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new BucketDispatcher(fakeServiceProvider);

		var command = new StubBucketCommand();

		var fakeServiceScopeFactory = Substitute.For<IServiceScopeFactory>();
		fakeServiceProvider.GetService<IServiceScopeFactory>()
			.Returns(fakeServiceScopeFactory);

		var fakeScope = Substitute.For<IServiceScope>();
		fakeScope.ServiceProvider
			.Returns(fakeServiceProvider);
		fakeServiceScopeFactory.CreateScope()
			.Returns(fakeScope);

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		fakeServiceProvider.GetService<ISerialNumberGenerator>()
			.Returns(fakeSerialNumberGenerator);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		fakeServiceProvider.GetService<ITgsRepository>()
			.Returns(fakeTgsRepository);

		await sut.PushCommandAsync("test1", command);

		await command.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.IsTrue(command.IsExcuted);
	}
}
