using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Reflection.Randomness { // пространство имен реализации случайности
    public class FromDistribution : Attribute { // класс производный от Attribute
        public FromDistribution(Type distributionType) { // конструктор типа распределения, без параметров
            ValidateType(distributionType);
        }

        public FromDistribution(Type distributionType, double x) { // конструктор типа распределения, с одним параметром
            ValidateType(distributionType);
        }

        public FromDistribution(Type distributionType, double x, double y) { // конструктор типа распределения, с двумя параметрами
            ValidateType(distributionType);
        }

        public FromDistribution(Type distributionType, double x, double y, double z) { // конструктор типа распределения, с тремя параметрами
            throw new ArgumentException($"{distributionType.FullName}"); // недействительный, исключение
        }

        private void ValidateType(Type distributionType) { // метод проверки корректности выбранного типа распределения
            if (distributionType != typeof(NormalDistribution) && distributionType != typeof(ExponentialDistribution)) { // распределение не нормальное или экспоненциальное
                throw new ArgumentException($"{distributionType.FullName}"); // исключение
            }
        }
    }

    public class Generator<TTarget> where TTarget : new() {  // класс с параметром типа TTarget, который должен иметь конструктор по умолчанию
        private static PropertyInfo[] TargetProperties { get; set; } // свойство - содержит все свойства типа TTarget, помеченные атрибутом FromDistribution

        private readonly Dictionary<PropertyInfo, IContinuousDistribution> propertyDistributions = new Dictionary<PropertyInfo, IContinuousDistribution>(); // словарь - отображения свойств объекта TTarget и соответствующие распределения

        public Generator() { // конструктор класса 
            TargetProperties = typeof(TTarget)
                .GetProperties() // получение свойств TTarget
                .Where(p => p.GetCustomAttributes(typeof(FromDistribution), false).Length != 0) // оставляются свойства, помеченные атрибутом
                .ToArray(); // результат в массив
        }

        private IContinuousDistribution CreateDistribution(PropertyInfo property) { // создание и возврат объекта интерфейса

            var customAttribute = property.CustomAttributes.FirstOrDefault(); // получение настраиваемого атрибута, соответствующего данному свойству

            if (customAttribute == null) return null; // атрибут не найден - null

            var attributeArgs = customAttribute.ConstructorArguments; // получение значений атрибута
            var distributionType = (Type)attributeArgs[0].Value; // получение типа распределения
            var values = attributeArgs.Skip(1).Select(a => a.Value).ToArray(); // получение значений параметров
            var distribution = (IContinuousDistribution)Activator.CreateInstance(distributionType, values); // создание объекта типа распределения
            propertyDistributions.Add(property, distribution); // полученное распределение в словарь
            return distribution; // возвращается полученное распределение
        }

        private void SetValue(PropertyInfo property, TTarget targetObject, IContinuousDistribution distribution, Random rnd) { // метод установки случайного значения свойства TTarget
            property.SetValue(targetObject, distribution.Generate(rnd)); // случайное значение, полученное с помощью распределения
        }

        public TTarget Generate(Random rnd) { // метод генерации объекта заполненного случайным образом 

            var targetObject = new TTarget(); // новый объект TTarget

            foreach (var property in TargetProperties) {  // для каждого свойства случайное значение в соответствии с распределением

                IContinuousDistribution distribution; // распределение для текущего свойства

                if (propertyDistributions.TryGetValue(property, out distribution)) { // если для текущего свойства уже было создано распределение
                    SetValue(property, targetObject, distribution, rnd); // используется оно
                } else { // ранее не было создано распределение
                    distribution = CreateDistribution(property);// создается 
                    if (distribution != null) {
                        SetValue(property, targetObject, distribution, rnd); // и используется
                    }
                }
            }

            return targetObject; // возвращается готовый объект TTarget
        }
    }
}