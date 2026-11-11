namespace MES.Contracts;

public sealed record NavigationMenuTreeResponse(IReadOnlyList<NavigationModuleDto> Modules);

public sealed record NavigationModuleDto(
    string ModuleNo,
    string ModuleName,
    decimal ModuleOrder,
    IReadOnlyList<NavigationMenuGroupDto> MenuGroups);

public sealed record NavigationMenuGroupDto(
    string MenuNo,
    string MenuName,
    decimal MenuOrder,
    IReadOnlyList<NavigationMenuItemDto> Items);

public sealed record NavigationMenuItemDto(
    string MenuNo,
    string MenuName,
    string? FunctionNo,
    string? FunctionName,
    decimal FunctionOrder);
