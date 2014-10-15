using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web.ModelBinding;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace MappingDemo
{
    public class User
    {
        [Required]
        public string Name { get; set; }
    }

    public class UserViewModel
    {
        public string Name { get; set; }
    }

    public class Mapping
    {
        [Fact]
        public void ConvertWithoutMetaDataTest()
        {
            var user = new User {Name = "Foo"};
            Mapper.CreateMap<User, UserViewModel>();
            UserViewModel userViewModel = Mapper.Map<User, UserViewModel>(user);
            userViewModel.Name.Should().Be("Foo");

            var requiredAttribute = user.GetAttributeFrom<RequiredAttribute>("Name");
            requiredAttribute.Should().NotBeNull();

            var requiredAttributeVM = userViewModel.GetAttributeFrom<RequiredAttribute>("Name");
            requiredAttributeVM.Should().NotBeNull();
        }

        [Fact]
        public void ConvertWithMetaDataTest()
        {
            var user = new User { Name = "Foo" };
            Mapper.CreateMap<User, UserViewModel>();
            UserViewModel userViewModel = Mapper.Map<User, UserViewModel>(user);

            IConfigurationProvider configurationProvider = Mapper.Engine.ConfigurationProvider;

            MetadataProvider metadataProvider = new MetadataProvider(configurationProvider);
            
            ValidatorProvider validatorProvider = new ValidatorProvider(configurationProvider);
            //validatorProvider.GetValidators()

            //Mapper.Initialize(cfg => cfg.);
        }
    }

    public class MetadataProvider : DataAnnotationsModelMetadataProvider
    {
        private readonly IConfigurationProvider _mapper;

        public MetadataProvider(IConfigurationProvider mapper)
        {
            _mapper = mapper;
        }

        protected override ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
        {
            //Grab attributes from the entity columns and copy them to the view model
            var mappedAttributes = _mapper.GetMappedAttributes(containerType, propertyName, attributes);

            ModelMetadata modelMetadata = base.CreateMetadata(mappedAttributes, containerType, modelAccessor, modelType, propertyName);

            return modelMetadata;
        }
    }

    public class ValidatorProvider : DataAnnotationsModelValidatorProvider
    {
        private readonly IConfigurationProvider _mapper;

        public ValidatorProvider(IConfigurationProvider mapper)
        {
            _mapper = mapper;
        }

        protected override IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ModelBindingExecutionContext context, IEnumerable<Attribute> attributes)
        {
            var mappedAttributes = _mapper.GetMappedAttributes(metadata.ContainerType, metadata.PropertyName, attributes);
            return base.GetValidators(metadata, context, mappedAttributes);
        }
    }

    public static class Helper
    {
        public static T GetAttributeFrom<T>(this object instance, string propertyName) where T : Attribute
        {
            Type attrType = typeof (T);
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            return (T) property.GetCustomAttributes(attrType, false).First();
        }

        public static IEnumerable<Attribute> GetMappedAttributes(this IConfigurationProvider mapper, Type sourceType, string propertyName, IEnumerable<Attribute> existingAttributes)
        {
            if (sourceType != null)
            {
                foreach (TypeMap typeMap in mapper.GetAllTypeMaps().Where(i => i.SourceType == sourceType))
                {
                    foreach (PropertyMap propertyMap in typeMap.GetPropertyMaps())
                    {
                        if (propertyMap.IsIgnored() || propertyMap.SourceMember == null)
                            continue;

                        if (propertyMap.SourceMember.Name == propertyName)
                        {
                            foreach (ValidationAttribute attribute in propertyMap.DestinationProperty.MemberInfo.GetCustomAttributes(typeof (ValidationAttribute), true))
                            {
                                if (!existingAttributes.Any(i => i.GetType() == attribute.GetType()))
                                    yield return attribute;
                            }
                        }
                    }
                }
            }

            if (existingAttributes != null)
            {
                foreach (Attribute attribute in existingAttributes)
                {
                    yield return attribute;
                }
            }
        }
    }
}