using System.Collections.Generic;

namespace peliculasAPI.DTOs
{
    public class LandingPageDTO
    {
        public List<PeliculaDTO> EnCines { get; set; }

        public List<PeliculaDTO> ProximosEstrenos { get; set; }
    }
}