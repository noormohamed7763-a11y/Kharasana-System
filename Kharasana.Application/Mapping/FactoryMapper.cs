using Kharasana.Application.DTOs.Factory;
using Kharasana.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Kharasana.Application.Mapping;

/// <summary>
/// مُحوّل كائنات المصانع المعتمد على التوليد المصدري أثناء التجميع.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class FactoryMapper
{
    public static partial FactoryDto ToDto(Factory factory);
}
