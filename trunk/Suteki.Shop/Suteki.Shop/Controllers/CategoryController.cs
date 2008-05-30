﻿using System.Web.Mvc;
using Suteki.Common.Repositories;
using Suteki.Common.Services;
using Suteki.Common.Validation;
using Suteki.Shop.ViewData;
using Suteki.Shop.Repositories;
using System.Security.Permissions;

namespace Suteki.Shop.Controllers
{
    public class CategoryController : ControllerBase
    {
        IRepository<Category> categoryRepository;
        IOrderableService<Category> orderableService;

        public CategoryController(
            IRepository<Category> categoryRepository,
            IOrderableService<Category> orderableService)
        {
            this.categoryRepository = categoryRepository;
            this.orderableService = orderableService;
        }

        public ActionResult Index()
        {
            return RenderIndexView();
        }

        private ActionResult RenderIndexView()
        {
            Category root = categoryRepository.GetRootCategory();
            return View("Index", ShopView.Data.WithCategory(root));
        }

        [PrincipalPermission(SecurityAction.Demand, Role = "Administrator")]
        public ActionResult New(int id)
        {
            Category defaultCategory = new Category 
            { 
                ParentId = id,
                Position = orderableService.NextPosition
            };
            return View("Edit", EditViewData.WithCategory(defaultCategory)); 
        }

        [PrincipalPermission(SecurityAction.Demand, Role = "Administrator")]
        public ActionResult Edit(int id)
        {
            Category category = categoryRepository.GetById(id);
            return View("Edit", EditViewData.WithCategory(category));
        }

        [PrincipalPermission(SecurityAction.Demand, Role = "Administrator")]
        public ActionResult Update(int categoryId)
        {
            Category category = null;
            if (categoryId == 0)
            {
                category = new Category();
            }
            else
            {
                category = categoryRepository.GetById(categoryId);
            }

            try
            {
                ValidatingBinder.UpdateFrom(category, Request.Form);
            }
            catch (ValidationException validationException)
            {
                return View("Edit", EditViewData.WithCategory(category)
                    .WithErrorMessage(validationException.Message));
            }

            if (categoryId == 0)
            {
                categoryRepository.InsertOnSubmit(category);
            }

            categoryRepository.SubmitChanges();

            return View("Edit", EditViewData.WithCategory(category).WithMessage("The category has been saved"));
        }

        private ShopViewData EditViewData
        {
            get
            {
                return ShopView.Data.WithCategories(categoryRepository.GetAll().Alphabetical());
            }
        }

        [PrincipalPermission(SecurityAction.Demand, Role = "Administrator")]
        public ActionResult MoveUp(int id)
        {
            MoveThis(id).UpOne();
            return RenderIndexView();
        }

        [PrincipalPermission(SecurityAction.Demand, Role = "Administrator")]
        public ActionResult MoveDown(int id)
        {
            MoveThis(id).DownOne();
            return RenderIndexView();
        }

        private IOrderServiceWithConstrainedPosition<Category> MoveThis(int id)
        {
            Category category = categoryRepository.GetById(id);
            return orderableService
                .MoveItemAtPosition(category.Position)
                .ConstrainedBy(c => c.ParentId == category.ParentId);
        }
    }
}