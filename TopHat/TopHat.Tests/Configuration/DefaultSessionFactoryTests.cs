﻿namespace TopHat.Tests.Configuration {
    using System.Data;

    using Moq;

    using TopHat.CodeGeneration;
    using TopHat.Configuration;
    using TopHat.Engine;

    using Xunit;

    public class DefaultSessionFactoryTests {
        private readonly Mock<IEngine> mockEngine = new Mock<IEngine>();

        private readonly Mock<IDbConnection> mockConnection = new Mock<IDbConnection>();

        private readonly Mock<IDbTransaction> mockTransaction = new Mock<IDbTransaction>();

        private readonly Mock<IGeneratedCodeManager> mockCodeManager = new Mock<IGeneratedCodeManager>();

        [Fact]
        public void CreateReturnsASession() {
            var target = this.MakeTarget();
            Assert.IsType<Session>(target.Create(this.mockEngine.Object, this.mockConnection.Object, this.mockCodeManager.Object));
        }

        [Fact]
        public void CreateWithTransactionReturnsASession() {
            var target = this.MakeTarget();
            Assert.IsType<Session>(target.Create(this.mockEngine.Object, this.mockConnection.Object, this.mockCodeManager.Object, this.mockTransaction.Object));
        }

        private DefaultSessionFactory MakeTarget() {
            return new DefaultSessionFactory();
        }
    }
}