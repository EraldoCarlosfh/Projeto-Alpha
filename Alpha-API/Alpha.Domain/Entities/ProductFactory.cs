﻿using Alpha.Domain.Commands.Users;
using Alpha.Framework.MediatR.EventSourcing.Entity;
using Alpha.Framework.MediatR.Resources.Extensions;
using System;

namespace Alpha.Domain.Entities
{
    public partial class Product
    {
        public static Product Create(string name,
                                  string barCode,
                                  string image,
                                  decimal? price,
                                  string id = null)
        {
            return new Product
            {
                Id = id == null ? new EntityId(Guid.NewGuid().ToString()) : id.ToEntityId(),
                Name = name,
                BarCode = barCode,
                Price = price,
                IsActive = true
            };
        }

        public static Product Create(
            CreateProductCommand command)
        {
            return new Product
            {
                Name = command.Name,
                BarCode = command.BarCode,
                Price = command.Price,
                IsActive = true,
                Image = command.Image
            };
        }       

        public void SetImage(string image)
        {
            Image = image;
        }

        public void UpdateActivated(bool activated)
        {
            IsActive = activated;
        }

        public void UpdateName(string name)
        {
            Name = name;
        }

        public void Update(UpdateProductCommand command)
        {
            Name = command.Name;
            BarCode = command.BarCode;
            Image = command.Image;
            Price = command.Price;
        }
    }
}
