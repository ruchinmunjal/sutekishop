﻿using System;
using NUnit.Framework;
using Moq;
using Suteki.Shop.Controllers;
using System.Web.Mvc;
using Suteki.Shop.ViewData;
using Suteki.Shop.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;

namespace Suteki.Shop.Tests.Controllers
{
    [TestFixture]
    public class OrderControllerTests
    {
        OrderController orderController;

        IRepository<Order> orderRepository;
        IRepository<Basket> basketRepository;
        IRepository<Country> countryRepository;
        IRepository<CardType> cardTypeRepository;

        ControllerTestContext testContext;

        [SetUp]
        public void SetUp()
        {
            orderRepository = new Mock<IRepository<Order>>().Object;
            basketRepository = new Mock<IRepository<Basket>>().Object;
            countryRepository = new Mock<IRepository<Country>>().Object;
            cardTypeRepository = new Mock<IRepository<CardType>>().Object;

            orderController = new OrderController(
                orderRepository,
                basketRepository,
                countryRepository,
                cardTypeRepository);

            testContext = new ControllerTestContext(orderController);
        }

        [Test]
        public void Checkout_ShouldDisplayCheckoutForm()
        {
            int basketId = 6;

            Basket basket = new Basket { BasketId = basketId };
            IQueryable<Country> countries = new List<Country> { new Country() }.AsQueryable();
            IQueryable<CardType> cardTypes = new List<CardType> { new CardType() }.AsQueryable();

            // expectations
            Mock.Get(basketRepository).Expect(br => br.GetById(basketId))
                .Returns(basket)
                .Verifiable();

            Mock.Get(countryRepository).Expect(cr => cr.GetAll())
                .Returns(countries)
                .Verifiable();

            Mock.Get(cardTypeRepository).Expect(ctr => ctr.GetAll())
                .Returns(cardTypes)
                .Verifiable();


            // exercise Checkout action
            orderController.Checkout(basketId)
                .ReturnsRenderViewResult()
                .AssertNotNull(vd => vd.Order)
                .AssertNotNull(vd => vd.Order.Contact)
                .AssertNotNull(vd => vd.Order.Contact1)
                .AssertNotNull(vd => vd.Order.Card)
                .AssertAreSame(basket, vd => vd.Order.Basket)
                .AssertNotNull(vd => vd.Countries)
                .AssertAreSame(cardTypes, vd => vd.CardTypes);

            Mock.Get(basketRepository).Verify();
            Mock.Get(countryRepository).Verify();
            Mock.Get(cardTypeRepository).Verify();
        }

        [Test]
        public void PlaceOrder_ShouldCreateANewOrder()
        {
            // mock the request form
            NameValueCollection form = BuildPlaceOrderRequest();
            testContext.TestContext.RequestMock.ExpectGet(r => r.Form).Returns(() => form);

            // expectations
            Mock.Get(orderRepository).Expect(or => or.InsertOnSubmit(It.IsAny<Order>())).Verifiable();
            Mock.Get(orderRepository).Expect(or => or.SubmitChanges()).Verifiable();

            // exercise PlaceOrder action
            RenderViewResult result = orderController.PlaceOrder() as RenderViewResult;

            // Assertions
            Assert.IsNotNull(result, "result is not a RenderViewResult");

            Assert.AreNotEqual("Checkout", result.ViewName, ((ShopViewData)result.ViewData).ErrorMessage);
            Assert.AreEqual("Item", result.ViewName, "View name is not 'Item'");

            ShopViewData viewData = result.ViewData as ShopViewData;
            Assert.IsNotNull(viewData, "view data is not ShopViewData");

            Order order = viewData.Order;
            Assert.IsNotNull(order, "The view data order is null");

            // Order
            Assert.AreEqual(10, order.OrderId);
            Assert.AreEqual(form["order.email"], order.Email);
            Assert.AreEqual(form["order.additionalinformation"], order.AdditionalInformation);
            Assert.IsFalse(order.UseCardHolderContact);
            Assert.IsFalse(order.PayByTelephone);

            // Card Contact
            Contact cardContact = order.Contact;
            AssertContactIsCorrect(form, cardContact, "cardcontact");

            // Delivery Contact
            Contact deliveryContact = order.Contact1;
            AssertContactIsCorrect(form, deliveryContact, "deliverycontact");

            // Card
            Card card = order.Card;
            Assert.IsNotNull(card, "card is null");
            Assert.AreEqual(form["card.cardtypeid"], card.CardTypeId.ToString());
            Assert.AreEqual(form["card.holder"], card.Holder);
            Assert.AreEqual(form["card.number"], card.Number);
            Assert.AreEqual(form["card.expirymonth"], card.ExpiryMonth.ToString());
            Assert.AreEqual(form["card.expiryyear"], card.ExpiryYear.ToString());
            Assert.AreEqual(form["card.startmonth"], card.StartMonth.ToString());
            Assert.AreEqual(form["card.startyear"], card.StartYear.ToString());
            Assert.AreEqual(form["card.issuenumber"], card.IssueNumber.ToString());
            Assert.AreEqual(form["card.securitycode"], card.SecurityCode.ToString());

            Mock.Get(orderRepository).Verify();
        }

        private static void AssertContactIsCorrect(NameValueCollection form, Contact contact, string prefix)
        {
            Assert.IsNotNull(contact, prefix + " is null");
            Assert.AreEqual(form[prefix + ".firstname"], contact.Firstname);
            Assert.AreEqual(form[prefix + ".lastname"], contact.Lastname);
            Assert.AreEqual(form[prefix + ".address1"], contact.Address1);
            Assert.AreEqual(form[prefix + ".address2"], contact.Address2);
            Assert.AreEqual(form[prefix + ".address3"], contact.Address3);
            Assert.AreEqual(form[prefix + ".town"], contact.Town);
            Assert.AreEqual(form[prefix + ".county"], contact.County);
            Assert.AreEqual(form[prefix + ".postcode"], contact.Postcode);
            Assert.AreEqual(form[prefix + ".countryid"], contact.CountryId.ToString());
            Assert.AreEqual(form[prefix + ".telephone"], contact.Telephone);
        }

        private static NameValueCollection BuildPlaceOrderRequest()
        {
            NameValueCollection form = new NameValueCollection();
            form.Add("order.orderid", "10");
            form.Add("order.basketid", "22");

            form.Add("cardcontact.firstname", "Mike");
            form.Add("cardcontact.lastname", "Hadlow");
            form.Add("cardcontact.address1", "23 The Street");
            form.Add("cardcontact.address2", "The Manor");
            form.Add("cardcontact.address3", "");
            form.Add("cardcontact.town", "Hove");
            form.Add("cardcontact.county", "East Sussex");
            form.Add("cardcontact.postcode", "BN6 2EE");
            form.Add("cardcontact.countryid", "1");
            form.Add("cardcontact.telephone", "01273 234234");

            form.Add("order.email", "mike@mike.com");
            form.Add("emailconfirm", "mike@mike.com");

            form.Add("order.usecardholdercontact", "False");

            form.Add("deliverycontact.firstname", "Mike");
            form.Add("deliverycontact.lastname", "Hadlow");
            form.Add("deliverycontact.address1", "23 The Street");
            form.Add("deliverycontact.address2", "The Manor");
            form.Add("deliverycontact.address3", "");
            form.Add("deliverycontact.town", "Hove");
            form.Add("deliverycontact.county", "East Sussex");
            form.Add("deliverycontact.postcode", "BN6 2EE");
            form.Add("deliverycontact.countryid", "1");
            form.Add("deliverycontact.telephone", "01273 234234");

            form.Add("order.additionalinformation", "some more info");

            form.Add("card.cardtypeid", "1");
            form.Add("card.holder", "MR M HADLOW");
            form.Add("card.number", "1111111111111117");
            form.Add("card.expirymonth", "3");
            form.Add("card.expiryyear", "2009");
            form.Add("card.startmonth", "2");
            form.Add("card.startyear", "2003");
            form.Add("card.issuenumber", "3");
            form.Add("card.securitycode", "235");

            form.Add("order.paybytelephone", "False");
            return form;
        }
    }
}