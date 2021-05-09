using System;
using AutoMapper;
using peliculasAPI.DTOs;
using peliculasAPI.Entidades;

namespace peliculasAPI.Utilidades
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Genero, GeneroDTO>().ReverseMap();

            CreateMap<GeneroCreacionDTO, Genero>();
        }
    }
}
