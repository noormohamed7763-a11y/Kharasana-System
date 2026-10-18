using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Kharasana.Application.Mapping;

/// <summary>
/// مُحوّل كائنات أنواع الخرسانة المعتمد على التوليد المصدري أثناء التجميع.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ConcreteTypeMapper
{
    public static ConcreteTypeDto ToDto(ConcreteType concreteType)
    {
        var dto = MapInternal(concreteType);
        dto.FactoryName = concreteType.Factory?.FactoryName ?? string.Empty;
        return dto;
    }

    [MapperIgnoreTarget(nameof(ConcreteTypeDto.FactoryName))]
    private static partial ConcreteTypeDto MapInternal(ConcreteType concreteType);
}
