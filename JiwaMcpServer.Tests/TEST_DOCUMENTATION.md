# JiwaMCPServer Test Suite Documentation

## Overview
This document describes the comprehensive test suite created for the JiwaMCPServer project.

## Test Projects Created

### 1. **ConfigTests.cs**
Tests for the `Config` static class that manages Jiwa API configuration.

**Test Coverage:**
- API URL can be set and retrieved
- API Key can be set and retrieved
- Null/empty value handling
- Various valid URL formats
- Configuration state management

**Key Test Methods:**
- `JiwaAPIURL_CanBeSet()`
- `JiwaAPIKey_CanBeSet()`
- `JiwaAPIURL_AcceptsVariousValidUrls()`

### 2. **JiwaApiClientTests.cs**
Tests for the `JiwaApiClient` static service that handles API communication.

**Test Coverage:**
- AsyncLocal API key management
- Configuration validation
- HTTP method validation (GET, POST, PATCH)
- Error handling when API URL is not configured
- Async context isolation

**Key Test Methods:**
- `CurrentApiKey_CanBeSet()`
- `CurrentApiKey_IsAsyncLocal()`
- `GetAsync_ThrowsWhenJiwaAPIURLNotConfigured()`
- `PostAsync_ThrowsWhenJiwaAPIURLNotConfigured()`
- `GetRawBytesAsync_ThrowsWhenJiwaAPIURLNotConfigured()`

### 3. **JiwaToolBaseTests.cs**
Tests for the `JiwaToolBase` abstract class utility methods.

**Test Coverage:**
- DTO type resolution by name and fully qualified name
- Case-insensitive type name matching
- DTO schema generation
- Include statement validation for query results
- Input validation (null, empty, whitespace)

**Key Test Methods:**
- `ResolveJiwaDtoType_FindsValidDtoByName()`
- `ResolveJiwaDtoType_IsCaseInsensitive()`
- `CreateDtoSchema_ReturnsJsonSchema()`
- `EnsureIncludeContainsTotal_AddsWhenMissing()`
- `EnsureIncludeContainsTotal_DoesNotDuplicateWhenPresent()`

**Helper Class:**
- `ToolBaseTestHelper` - Uses reflection to access protected members

### 4. **SchemaToolsTests.cs**
Tests for the `SchemaTools` MCP tool that provides DTO schema information.

**Test Coverage:**
- Schema retrieval for various DTO types
- Error handling for invalid DTOs
- Case-insensitive DTO type naming
- Input validation
- Theory-based testing for multiple DTO types

**Key Test Methods:**
- `GetDtoSchema_ReturnsSchemaForValidDtoName()`
- `GetDtoSchema_ReturnsErrorForInvalidDtoName()`
- `GetDtoSchema_IsCaseInsensitive()`
- `GetDtoSchema_WorksWithVariousJiwaDtoTypes()`

### 5. **CustomerToolsTests.cs**
Tests for the `CustomerTools` MCP tool class for customer/debtor operations.

**Test Coverage:**
- Search customer functionality
- Get customer details
- Customer transactions retrieval
- Customer classifications and categories searches
- Large result set confirmation flow
- Cancellation token support

**Key Test Methods:**
- `SearchCustomers_ReturnsStringResult()`
- `GetCustomer_ReturnsStringResult()`
- `GetCustomerTransactions_ReturnsStringResult()`
- `SearchCustomerClassifications_ReturnsStringResult()`
- `SearchCustomers_WithConfirmationButNoToken_ReturnsWarning()`

### 6. **ProductToolsTests.cs**
Tests for the `ProductTools` MCP tool class for inventory/product operations.

**Test Coverage:**
- Product search
- Product details retrieval
- Stock on hand information
- Product classifications and categories
- Product picture retrieval with various parameter combinations
- Parameter preference handling (InventoryID over PartNo)

**Key Test Methods:**
- `SearchProducts_ReturnsStringResult()`
- `GetProduct_ReturnsStringResult()`
- `GetStockOnHand_ReturnsStringResult()`
- `GetProductPicture_WithoutParameters_ReturnsError()`
- `GetProductPicture_WithInventoryID_ReturnsContentBlock()`
- `GetProductPicture_PrefersInventoryIDOverPartNo()`

### 7. **SupplierToolsTests.cs**
Tests for the `SupplierTools` MCP tool class for supplier/creditor operations.

**Test Coverage:**
- Supplier search
- Supplier details retrieval
- Creditor classifications
- Large result set confirmation with tokens
- Cancellation token handling

**Key Test Methods:**
- `SearchSuppliers_ReturnsStringResult()`
- `GetSupplier_ReturnsStringResult()`
- `SearchCreditorClassifications_ReturnsStringResult()`
- `SearchSuppliers_WithCancellation_ReturnsResult()`

### 8. **PurchaseOrderToolsTests.cs**
Tests for the `PurchaseOrderTools` MCP tool class for purchase order operations.

**Test Coverage:**
- Purchase order retrieval
- Purchase order creation
- Purchase order modification
- Purchase order line addition
- Purchase information search
- Purchase search with confirmation tokens
- Cancellation token handling

**Key Test Methods:**
- `GetPurchaseOrder_ReturnsStringResult()`
- `CreatePurchaseOrder_ReturnsStringResult()`
- `ModifyPurchaseOrder_ReturnsStringResult()`
- `SearchPurchaseInformation_WithConfirmation_ReturnsStringResult()`
- `GetPurchaseOrder_WithCancellation_ReturnsResult()`

### 9. **SalesOrderToolsTests.cs**
Tests for the `SalesOrderTools` MCP tool class for sales order operations.

**Test Coverage:**
- Sales order retrieval
- Sales order creation
- Sales order modification
- Sales order line addition
- Sales information search
- Sales search with confirmation tokens
- Cancellation token handling

**Key Test Methods:**
- `GetSalesOrder_ReturnsStringResult()`
- `CreateSalesOrder_ReturnsStringResult()`
- `ModifySalesOrder_ReturnsStringResult()`
- `SearchSalesInformation_WithConfirmation_ReturnsStringResult()`
- `GetSalesOrder_WithCancellation_ReturnsResult()`

### 10. **JiwaToolBasePaginationTests.cs**
Integration tests for pagination and large result set confirmation logic.

**Test Coverage:**
- Page size validation
- Negative and zero page size error handling
- Valid page size acceptance
- Pagination workflow

**Key Test Methods:**
- `GetAllQueryResultsAsync_ThrowsWithNegativePageSize()`
- `GetAllQueryResultsAsync_ThrowsWithZeroPageSize()`
- `GetAllQueryResultsAsync_AcceptsValidPageSizes()`

### 11. **ToolsErrorHandlingTests.cs**
Comprehensive error handling and edge case tests across all tool classes.

**Test Coverage:**
- Null input handling
- API misconfiguration error responses
- Concurrent method execution
- Empty and whitespace parameter handling
- Error message consistency
- Configuration state effects on all tools

**Key Test Methods:**
- `CustomerTools_ReturnsErrorMessageOnApiFailure()`
- `MultipleToolMethodsConcurrently()`
- `ProductTools_GetProductPicture_WithEmptyBothParameters_ReturnsError()`
- `ToolsErrorHandlingTests` covering all major tool classes

## Test Statistics

| Component | Test Count | Coverage Areas |
|-----------|-----------|-----------------|
| Config | 7 | Configuration management |
| JiwaApiClient | 7 | API communication, error handling |
| JiwaToolBase | 11 | DTO resolution, schema generation |
| SchemaTools | 7 | DTO schema retrieval |
| CustomerTools | 6 | Customer operations |
| ProductTools | 7 | Product/inventory operations |
| SupplierTools | 5 | Supplier operations |
| PurchaseOrderTools | 8 | Purchase order operations |
| SalesOrderTools | 8 | Sales order operations |
| Pagination | 4 | Pagination logic |
| Error Handling | 10 | Error scenarios |
| **Total** | **80+** | **Comprehensive coverage** |

## Test Execution

### Running All Tests
```powershell
dotnet test JiwaMcpServer.Tests/JiwaMcpServer.Tests.csproj
```

### Running Tests for a Specific Class
```powershell
dotnet test --filter ClassName=JiwaMcpServer.Tests.ConfigTests
```

### Running Tests with Code Coverage
```powershell
dotnet test JiwaMcpServer.Tests/JiwaMcpServer.Tests.csproj /p:CollectCoverage=true
```

### Running Specific Test
```powershell
dotnet test --filter "FullyQualifiedName=JiwaMcpServer.Tests.ConfigTests.JiwaAPIURL_CanBeSet"
```

## NuGet Packages Used

- **xUnit** (2.9.3) - Testing framework
- **Microsoft.NET.Test.Sdk** (17.14.1) - Test runtime
- **xunit.runner.visualstudio** (3.1.4) - Visual Studio test adapter
- **coverlet.collector** (6.0.4) - Code coverage
- **Moq** (4.20.71) - Mocking framework (for future advanced tests)

## Design Patterns & Best Practices

### 1. **Reflection-Based Helper Classes**
The `ToolBaseTestHelper` class uses reflection to access protected methods in `JiwaToolBase`, allowing comprehensive testing of private implementation details without exposing them publicly.

### 2. **Configuration State Management**
Tests manage configuration state by:
- Preserving original values before modification
- Restoring original values in finally blocks
- Testing both valid and invalid configurations

### 3. **Async/Await Testing**
Tests properly handle asynchronous method calls:
- Using `await` for async methods
- Testing cancellation token propagation
- Testing concurrent execution scenarios

### 4. **Error Path Testing**
Tests verify error conditions:
- Invalid configuration (null API URL)
- Invalid input parameters
- Missing required information
- API failures

### 5. **Theory-Based Testing**
Uses xUnit's `[Theory]` attribute with `[InlineData]` to test multiple scenarios with different inputs.

## Future Test Enhancements

### Mock Integration Tests
- Mock `HttpClient` to test API communication without real API
- Mock response scenarios (success, errors, timeouts)
- Test retry logic and resilience patterns

### Performance Tests
- Benchmark pagination with large result sets
- Measure DTO resolution performance
- Profile schema generation with complex types

### Integration Tests
- Real API integration tests (with test environment)
- End-to-end workflow tests
- Large result set confirmation token lifecycle

### UI/API Tests
- Web API endpoint testing
- MCP protocol compliance tests
- Request/response validation

## Test Maintenance

### Running Tests in CI/CD
Tests are designed to run in automated pipelines:
- No external dependencies required (except Jiwa API configuration)
- Fast execution time
- Deterministic results
- Clear error messages

### Adding New Tests
When adding new features:
1. Create test class in `JiwaMcpServer.Tests/`
2. Name following convention: `<FeatureName>Tests.cs`
3. Add tests covering:
   - Happy path scenarios
   - Error conditions
   - Edge cases
   - Input validation
4. Update this documentation

---

## Notes

- Tests are designed to work with .NET 10 as specified in the project configuration
- Many tests will fail gracefully when API endpoints are not configured, returning error messages instead of throwing exceptions
- The test suite follows AAA (Arrange-Act-Assert) pattern for consistency
- Tests use xUnit facts and theories for comprehensive scenario coverage
