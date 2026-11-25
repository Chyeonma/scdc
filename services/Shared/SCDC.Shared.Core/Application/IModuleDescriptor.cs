namespace SCDC.BuildingBlocks.Application;

public interface IModuleDescriptor
{
    string Name { get; }
    string DatabaseSchema { get; }
    ModuleStage Stage { get; }
}

public enum ModuleStage
{
    Foundation,
    Active
}
