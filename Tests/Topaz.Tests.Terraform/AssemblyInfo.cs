using NUnit.Framework;

// Keep Terraform tests serial to avoid provider startup contention in shared Docker test infrastructure.
[assembly: Parallelizable(ParallelScope.None)]
[assembly: LevelOfParallelism(1)]
