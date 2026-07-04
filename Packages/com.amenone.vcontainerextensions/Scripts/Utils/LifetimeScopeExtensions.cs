using System;
using System.Linq;
using Amenone.VcontainerExtensions.Identifier;
using Amenone.VcontainerExtensions.AdditionalUtils;
using VContainer;

namespace Amenone.VcontainerExtensions.Utils
{
    public static class LifetimeScopeExtensions
    {
        public static RegistrationBuilder RegisterComponentOrNullObFromArray<TToFind, NullT>(
            this IContainerBuilder builder, IRegisterMarker[] registerable, bool asSelf = true , bool asImplementedInterfaces = true)
        {
            var component = registerable.OfType<TToFind>().SingleOrDefault();

            RegistrationBuilder registrationBuilder;
            if (component == null)
                registrationBuilder = builder.Register<NullT>(Lifetime.Singleton);
            else
                registrationBuilder = builder.RegisterComponentWithoutSelf(component);

            if( asSelf )
                registrationBuilder.AsSelf();

            if (asImplementedInterfaces)
                registrationBuilder.AsImplementedInterfaces();

            return registrationBuilder;
        }

        public static RegistrationBuilder RegisterComponentFromArray<TToFind>(this IContainerBuilder builder,
            IRegisterMarker[] registerable, bool asSelf = true , bool asImplementedInterfaces = true)
        {
            var component = registerable.OfType<TToFind>().SingleOrDefault();

            if (component == null) throw new Exception($"Component of type {typeof(TToFind)} is not found");

            RegistrationBuilder registrationBuilder;
            registrationBuilder = builder.RegisterComponentWithoutSelf(component);

            if(asSelf)
                registrationBuilder.AsSelf();

            if (asImplementedInterfaces)
                registrationBuilder.AsImplementedInterfaces();

            return registrationBuilder;
        }

    }
}