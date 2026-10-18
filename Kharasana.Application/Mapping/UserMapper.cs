using Kharasana.Application.DTOs.User;
using Kharasana.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Kharasana.Application.Mapping;

/// <summary>
/// مُحوّل كائنات المستخدمين المعتمد على التوليد المصدري أثناء التجميع (Zero-allocation / Compile-time).
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class UserMapper
{
    [MapProperty(nameof(User.Factory.FactoryName), nameof(UserDto.FactoryName))]
    public static partial UserDto ToDto(User user);
}
