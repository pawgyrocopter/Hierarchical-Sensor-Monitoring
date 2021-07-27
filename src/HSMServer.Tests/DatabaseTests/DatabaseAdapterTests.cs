﻿using System;
using System.Collections.Generic;
using System.Linq;
using HSMServer.DataLayer.Model;
using HSMServer.Keys;
using HSMServer.Tests.Fixture;
using Xunit;

namespace HSMServer.Tests.DatabaseTests
{
    public class DatabaseAdapterTests : IClassFixture<DatabaseAdapterFixture>, IDisposable
    {
        private readonly DatabaseAdapterFixture _databaseFixture;
        public DatabaseAdapterTests(DatabaseAdapterFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
        }

        #region Product tests

        [Fact]
        public void ProductMustBeAdded()
        {
            //Arrange
            var product = _databaseFixture.GetFirstTestProduct();

            //Act
            _databaseFixture.DatabaseAdapter.AddProduct(product);
            var existingProduct = _databaseFixture.DatabaseAdapter.GetProduct(_databaseFixture.FirstProductName);

            //Assert
            Assert.Equal(product.Name, existingProduct.Name);
            Assert.Equal(product.Key, existingProduct.Key);
            Assert.Equal(product.DateAdded, existingProduct.DateAdded);
        }

        [Fact]
        public void ProductMustBeRemoved()
        {
            //Arrange
            var product = _databaseFixture.GetSecondTestProduct();

            //Act
            _databaseFixture.DatabaseAdapter.AddProduct(product);

            //Assert
            Assert.NotNull(_databaseFixture.DatabaseAdapter.GetProduct(_databaseFixture.SecondProductName));

            //Act
            _databaseFixture.DatabaseAdapter.RemoveProduct(product.Name);
            var correspondingProduct = _databaseFixture.DatabaseAdapter.GetProduct(product.Name);

            //Assert
            Assert.Null(correspondingProduct);
        }

        [Fact]
        public void ListMustReturnAllAddedProducts()
        {
            //Arrange
            var product = _databaseFixture.GetThirdTestProduct();

            //Act
            _databaseFixture.DatabaseAdapter.AddProduct(product);
            var list = _databaseFixture.GetProductsList();

            //Assert
            Assert.Contains(list, p => p.Name == product.Name && p.Key == product.Key);
        }

        [Fact]
        public void ExtraKeyMustBeAdded()
        {
            //Arrange
            var product = _databaseFixture.GetFirstTestProduct();
            var key = KeyGenerator.GenerateExtraProductKey(product.Name, _databaseFixture.ExtraKeyName);
            var extraKey = new ExtraProductKey {Key = key, Name = _databaseFixture.ExtraKeyName};
            product.ExtraKeys = new List<ExtraProductKey> { extraKey };

            //Act
            _databaseFixture.DatabaseAdapter.UpdateProduct(product);

            //Assert
            var gotProduct = _databaseFixture.DatabaseAdapter.GetProduct(product.Name);
            Assert.NotEmpty(gotProduct.ExtraKeys);
            Assert.Equal(extraKey.Name, gotProduct.ExtraKeys.First().Name);
            Assert.Equal(extraKey.Key, gotProduct.ExtraKeys.First().Key);
        }

        #endregion


        public void Dispose()
        {
            _databaseFixture.DatabaseAdapter.RemoveProduct(_databaseFixture.FirstProductName);
            _databaseFixture.DatabaseAdapter.RemoveProduct(_databaseFixture.SecondProductName);
            _databaseFixture.DatabaseAdapter.RemoveProduct(_databaseFixture.ThirdProductName);
            _databaseFixture?.Dispose();
        }
    }
}