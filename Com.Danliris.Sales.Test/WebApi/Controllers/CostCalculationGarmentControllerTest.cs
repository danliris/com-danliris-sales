﻿using AutoMapper;
using Com.Danliris.Sales.Test.WebApi.Utils;
using Com.Danliris.Service.Sales.Lib.BusinessLogic.Interface.CostCalculationGarmentLogic;
using Com.Danliris.Service.Sales.Lib.Models.CostCalculationGarments;
using Com.Danliris.Service.Sales.Lib.Services;
using Com.Danliris.Service.Sales.Lib.Utilities;
using Com.Danliris.Service.Sales.Lib.ViewModels.CostCalculationGarment;
using Com.Danliris.Service.Sales.WebApi.Controllers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Com.Danliris.Sales.Test.WebApi.Controllers
{
    public class CostCalculationGarmentControllerTest : BaseControllerTest<CostCalculationGarmentController, CostCalculationGarment, CostCalculationGarmentViewModel, ICostCalculationGarment>
    {
        [Fact]
        public void Get_PDF_NotFound()
        {
            var mocks = GetMocks();
            mocks.Facade.Setup(x => x.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(default(CostCalculationGarment));
            var controller = GetController(mocks);
            var response = controller.GetPDF(1).Result;

            int statusCode = this.GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.NotFound, statusCode);

        }

        [Fact]
        public void Get_PDF_Exception()
        {
            var mocks = GetMocks();
            mocks.Facade.Setup(x => x.ReadByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("error"));
            var controller = GetController(mocks);
            var response = controller.GetPDF(1).Result;

            int statusCode = this.GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);

        }

        [Fact]
        public void Get_PDF_Local_OK()
        {
            var mocks = GetMocks();

            var viewModel = this.ViewModel;

            mocks.Facade.Setup(x => x.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);
            mocks.Mapper.Setup(f => f.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarmentViewModel>())).Returns(viewModel);

            var controller = GetController(mocks);
            var response = controller.GetPDF(1).Result;

            Assert.NotNull(response);

        }

        [Fact]
        public async Task GetById_RO_Garment_Validation_NotNullModel_ReturnOK()
        {
            var ViewModel = this.ViewModel;
            ViewModel.CostCalculationGarment_Materials = new List<CostCalculationGarment_MaterialViewModel>();

            var mocks = GetMocks();
            mocks.Facade
                .Setup(f => f.ReadByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(Model);
            mocks.Facade
                .Setup(f => f.GetProductNames(It.IsAny<List<long>>()))
                .ReturnsAsync(new Dictionary<long, string>());
            mocks.Mapper
                .Setup(m => m.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarment>()))
                .Returns(ViewModel);

            var controller = GetController(mocks);
            var response = await controller.GetById_RO_Garment_Validation(It.IsAny<int>());

            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }

        [Fact]
        public async Task GetById_RO_Garment_Validation_NullModel_ReturnNotFound()
        {
            var mocks = GetMocks();
            mocks.Mapper.Setup(f => f.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarment>())).Returns(ViewModel);
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync((CostCalculationGarment)null);

            var controller = GetController(mocks);
            var response = await controller.GetById_RO_Garment_Validation(It.IsAny<int>());

            Assert.Equal((int)HttpStatusCode.NotFound, GetStatusCode(response));
        }

        [Fact]
        public async Task GetById_RO_Garment_Validation_ThrowException_ReturnInternalServerError()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception());

            var controller = GetController(mocks);
            var response = await controller.GetById_RO_Garment_Validation(It.IsAny<int>());

            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }

        [Fact]
        public void Validate_ViewModel()
        {
            List<CostCalculationGarmentViewModel> viewModels = new List<CostCalculationGarmentViewModel>
            {
                new CostCalculationGarmentViewModel()
                {
                    FabricAllowance = 0,
                    AccessoriesAllowance = 0
                },
                new CostCalculationGarmentViewModel()
                {
                    Quantity = 0,
                    DeliveryDate = DateTimeOffset.Now.AddDays(-1),
                    SMV_Cutting = 0,
                    SMV_Sewing = 0,
                    SMV_Finishing = 0,
                    ConfirmPrice = 0,
                    CostCalculationGarment_Materials = new List<CostCalculationGarment_MaterialViewModel>
                    {
                        new CostCalculationGarment_MaterialViewModel(),
                        new CostCalculationGarment_MaterialViewModel
                        {
                            Category = new CategoryViewModel { code = "CategoryCode" }
                        },
                        new CostCalculationGarment_MaterialViewModel
                        {
                            Category = new CategoryViewModel { code = "CategoryCode" },
                            Quantity = 0,
                            Conversion = 0
                        },
                        new CostCalculationGarment_MaterialViewModel
                        {
                            Category = new CategoryViewModel { code = "CategoryCode" },
                            UOMPrice = new UOMViewModel() { Unit = "Unit" },
                            UOMQuantity = new UOMViewModel() { Unit = "Unit" },
                            Conversion = 2
                        }
                    }
                }
            };

            foreach (var viewModel in viewModels)
            {
                var defaultValidationResult = viewModel.Validate(null);
                Assert.True(defaultValidationResult.Count() > 0);
            }
        }

        private async Task<int> GetStatusCodePatch((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<ICostCalculationGarment> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks, int id)
        {
            CostCalculationGarmentController controller = GetController(mocks);

            JsonPatchDocument<CostCalculationGarment> patch = new JsonPatchDocument<CostCalculationGarment>();
            IActionResult response = await controller.Patch(id, patch);

            return this.GetStatusCode(response);
        }

        [Fact]
        public async Task Patch_InvalidId_ReturnNotFound()
        {
            int statusCode = await this.GetStatusCodePatch(GetMocks(), 1);
            Assert.Equal((int)HttpStatusCode.NotFound, statusCode);
        }

        [Fact]
        public async Task Patch_ValidId_ReturnNoContent()
        {
            var mocks = GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<CostCalculationGarmentViewModel>())).Verifiable();
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);
            mocks.Facade.Setup(f => f.Patch(It.IsAny<long>(), It.IsAny<JsonPatchDocument<CostCalculationGarment>>())).ReturnsAsync(1);

            int statusCode = await this.GetStatusCodePatch(mocks, 1);
            Assert.Equal((int)HttpStatusCode.NoContent, statusCode);
        }

        [Fact]
        public async Task Patch_ThrowException_ReturnInternalServerError()
        {
            var mocks = GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<CostCalculationGarmentViewModel>())).Verifiable();
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);
            mocks.Facade.Setup(f => f.Patch(It.IsAny<long>(), It.IsAny<JsonPatchDocument<CostCalculationGarment>>())).ThrowsAsync(new Exception());

            int statusCode = await this.GetStatusCodePatch(mocks, 1);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        [Fact]
        public async Task Update_Ro_Sample()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<CostCalculationGarmentViewModel>())).Verifiable();
            var id = 1;
            var viewModel = new CostCalculationGarmentViewModel()
            {
                Id = id
            };
            mocks.Mapper.Setup(m => m.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarment>())).Returns(viewModel);
            mocks.Facade.Setup(f => f.UpdateAsync(It.IsAny<int>(), It.IsAny<CostCalculationGarment>())).ReturnsAsync(1);
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);

            var controller = GetController(mocks);
            var response = await controller.PutRoSample(id, It.IsAny<CostCalculationGarmentViewModel>());

            Assert.Equal((int)HttpStatusCode.NoContent, GetStatusCode(response));
        }

        [Fact]
        public async Task Update_Ro_Sample_ThrowException()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<CostCalculationGarmentViewModel>())).Verifiable();
            var id = 1;
            var viewModel = new CostCalculationGarmentViewModel()
            {
                Id = id
            };
            mocks.Mapper.Setup(m => m.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarment>())).Returns(viewModel);
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);
            mocks.Facade.Setup(f => f.UpdateAsync(It.IsAny<int>(), It.IsAny<CostCalculationGarment>())).ThrowsAsync(new Exception());

            var controller = GetController(mocks);
            var response = await controller.PutRoSample(id, It.IsAny<CostCalculationGarmentViewModel>());

            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }

        private int GetStatusCodeGet((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<ICostCalculationGarment> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            CostCalculationGarmentController controller = this.GetController(mocks);
            IActionResult response = controller.GetForROAcceptance();

            return this.GetStatusCode(response);
        }

        [Fact]
        public void Get_ForROAcceptance_WithoutException_ReturnOK()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadForROAcceptance(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new ReadResponse<CostCalculationGarment>(new List<CostCalculationGarment>(), 0, new Dictionary<string, string>(), new List<string>()));
            mocks.Mapper.Setup(f => f.Map<List<CostCalculationGarmentViewModel>>(It.IsAny<List<CostCalculationGarment>>())).Returns(this.ViewModels);

            int statusCode = this.GetStatusCodeGet(mocks);
            Assert.Equal((int)HttpStatusCode.OK, statusCode);
        }

        [Fact]
        public void Get_ForROAcceptance_ReadThrowException_ReturnInternalServerError()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadForROAcceptance(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception());

            int statusCode = this.GetStatusCodeGet(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        private async Task<int> GetAcceptCC((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<ICostCalculationGarment> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks, int id)
        {
            CostCalculationGarmentController controller = GetController(mocks);
            List<long> Id = new List<long> { 1 };
            IActionResult response = await controller.AcceptCC(Id);

            return this.GetStatusCode(response);
        }

        [Fact]
        public async Task Should_Success_AcceptCC()
        {
            var mocks = GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<CostCalculationGarmentViewModel>())).Verifiable();
            var id = 1;
            var viewModel = new CostCalculationGarmentViewModel()
            {
                Id = id
            };
            mocks.Mapper.Setup(m => m.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarment>())).Returns(viewModel);
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);
            mocks.Facade.Setup(f => f.UpdateAsync(It.IsAny<int>(), It.IsAny<CostCalculationGarment>())).ReturnsAsync(1);

            List<long> listId = new List<long> { viewModel.Id };

            var controller = GetController(mocks);
            var response = await controller.AcceptCC(listId);
            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }

        private int GetStatusCodeGetAvailable((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<ICostCalculationGarment> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            CostCalculationGarmentController controller = this.GetController(mocks);
            IActionResult response = controller.GetForROAvailable();

            return this.GetStatusCode(response);
        }

        [Fact]
        public void Get_ForROAvailable_WithoutException_ReturnOK()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadForROAvailable(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new ReadResponse<CostCalculationGarment>(new List<CostCalculationGarment>(), 0, new Dictionary<string, string>(), new List<string>()));
            mocks.Mapper.Setup(f => f.Map<List<CostCalculationGarmentViewModel>>(It.IsAny<List<CostCalculationGarment>>())).Returns(this.ViewModels);

            int statusCode = this.GetStatusCodeGetAvailable(mocks);
            Assert.Equal((int)HttpStatusCode.OK, statusCode);
        }

        [Fact]
        public void Get_ForROAvailable_ReadThrowException_ReturnInternalServerError()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadForROAvailable(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception());

            int statusCode = this.GetStatusCodeGetAvailable(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        private async Task<int> GetAvailableCC((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<ICostCalculationGarment> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks, int id)
        {
            CostCalculationGarmentController controller = GetController(mocks);
            List<long> Id = new List<long> { 1 };
            IActionResult response = await controller.AvailableCC(Id);

            return this.GetStatusCode(response);
        }

        [Fact]
        public async Task Should_Success_AvailableCC()
        {
            var mocks = GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<CostCalculationGarmentViewModel>())).Verifiable();
            var id = 1;
            var viewModel = new CostCalculationGarmentViewModel()
            {
                Id = id
            };
            mocks.Mapper.Setup(m => m.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarment>())).Returns(viewModel);
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);
            mocks.Facade.Setup(f => f.UpdateAsync(It.IsAny<int>(), It.IsAny<CostCalculationGarment>())).ReturnsAsync(1);

            List<long> listId = new List<long> { viewModel.Id };

            var controller = GetController(mocks);
            var response = await controller.AvailableCC(listId);
            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }

        private int GetStatusCodeGetDistribute((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<ICostCalculationGarment> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            CostCalculationGarmentController controller = this.GetController(mocks);
            IActionResult response = controller.GetForRODistribute();

            return this.GetStatusCode(response);
        }

        [Fact]
        public void Get_ForRODistribute_WithoutException_ReturnOK()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadForRODistribution(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new ReadResponse<CostCalculationGarment>(new List<CostCalculationGarment>(), 0, new Dictionary<string, string>(), new List<string>()));
            mocks.Mapper.Setup(f => f.Map<List<CostCalculationGarmentViewModel>>(It.IsAny<List<CostCalculationGarment>>())).Returns(this.ViewModels);

            int statusCode = this.GetStatusCodeGetDistribute(mocks);
            Assert.Equal((int)HttpStatusCode.OK, statusCode);
        }

        [Fact]
        public void Get_ForRODistribute_ReadThrowException_ReturnInternalServerError()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadForRODistribution(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception());

            int statusCode = this.GetStatusCodeGetDistribute(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        private async Task<int> GetDistributeCC((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<ICostCalculationGarment> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks, int id)
        {
            CostCalculationGarmentController controller = GetController(mocks);
            List<long> Id = new List<long> { 1 };
            IActionResult response = await controller.DistributeCC(Id);

            return this.GetStatusCode(response);
        }

        [Fact]
        public async Task Should_Success_DistributeCC()
        {
            var mocks = GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<CostCalculationGarmentViewModel>())).Verifiable();
            var id = 1;
            var viewModel = new CostCalculationGarmentViewModel()
            {
                Id = id
            };
            mocks.Mapper.Setup(m => m.Map<CostCalculationGarmentViewModel>(It.IsAny<CostCalculationGarment>())).Returns(viewModel);
            mocks.Facade.Setup(f => f.ReadByIdAsync(It.IsAny<int>())).ReturnsAsync(Model);
            mocks.Facade.Setup(f => f.UpdateAsync(It.IsAny<int>(), It.IsAny<CostCalculationGarment>())).ReturnsAsync(1);

            List<long> listId = new List<long> { viewModel.Id };

            var controller = GetController(mocks);
            var response = await controller.DistributeCC(listId);
            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }

        [Fact]
        public async Task PostCC_Success_ReturnNoContent()
        {
            var mocks = GetMocks();
            mocks.Facade.Setup(f => f.PostCC(It.IsAny<List<long>>())).ReturnsAsync(1);

            var controller = GetController(mocks);
            var response = await controller.PostCC(It.IsAny<List<long>>());

            var statusCode = GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.NoContent, statusCode);
        }

        [Fact]
        public async Task PostCC_NoChanges_ReturnInternalServerError()
        {
            var mocks = GetMocks();
            mocks.Facade.Setup(f => f.PostCC(It.IsAny<List<long>>())).ReturnsAsync(0);

            var controller = GetController(mocks);
            var response = await controller.PostCC(It.IsAny<List<long>>());

            var statusCode = GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        [Fact]
        public async Task PostCC_Failed_ReturnInternalServerError()
        {
            var mocks = GetMocks();
            mocks.Facade.Setup(f => f.PostCC(It.IsAny<List<long>>())).ThrowsAsync(new Exception(string.Empty));

            var controller = GetController(mocks);
            var response = await controller.PostCC(It.IsAny<List<long>>());

            var statusCode = GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        [Fact]
        public async Task UnpostCC_Success_ReturnNoContent()
        {
            var mocks = GetMocks();
            mocks.Facade.Setup(f => f.UnpostCC(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(1);

            var controller = GetController(mocks);
            var response = await controller.UnpostCC(It.IsAny<long>(), "Reason");

            var statusCode = GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.NoContent, statusCode);
        }

        [Fact]
        public async Task UnpostCC_Invalid_ReturnBadRequest()
        {
            var mocks = GetMocks();

            var controller = GetController(mocks);
            var response = await controller.UnpostCC(It.IsAny<long>(), null);

            var statusCode = GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        }

        [Fact]
        public async Task UnpostCC_Failed_ReturnInternalServerError()
        {
            var mocks = GetMocks();
            mocks.Facade.Setup(f => f.UnpostCC(It.IsAny<long>(), It.IsAny<string>())).ThrowsAsync(new Exception(string.Empty));

            var controller = GetController(mocks);
            var response = await controller.UnpostCC(It.IsAny<long>(), "Reason");

            var statusCode = GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }
    }
}
