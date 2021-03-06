﻿using AutoMapper;
using System;
using System.Globalization;
using System.Threading.Tasks;
using WeatherApp.BLL.HelperClasses;
using WeatherApp.BLL.Interfaces;
using WeatherApp.BLL.Models;

namespace WeatherApp.BLL.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly IMapper _mapper;
        private WeatherAPIProcessor _apiProcessorSingleton;
        public WeatherService(IMapper mapper)
        {
            _mapper = mapper;
            _apiProcessorSingleton = WeatherAPIProcessor.GetInstance();
        }

        public async Task<object> GetCurrentWeather(string apiKey, string cityName = null, int cityId = 0)
        {
            _apiProcessorSingleton.BaseAPIUrl = BaseAPIUrls.GET_CURRENT_WEATHER;
            object currentWeather;

            if (cityId > 0)
            {
                var query = $"id={cityId}&appid={apiKey}&units=imperial";
                currentWeather = await _apiProcessorSingleton.GetCurrentWeather(query);
            }
            else
            {
                var query = $"q={cityName}&appid={apiKey}&units=imperial";
                currentWeather = await _apiProcessorSingleton.GetCurrentWeather(query);
            }

            if (currentWeather != null)
            {
                if (currentWeather is string)
                {
                    return "invalid city name";
                }
                else
                {
                    var currentWeatherCast = (WeatherInfoRoot)currentWeather;
                    var currentWeatherDTO = _mapper.Map<WeatherInfoDTO>(currentWeatherCast);

                    var currentDateTime = GetDateTimeFromEpoch(
                        currentWeatherCast.Sys.Sunrise,
                        currentWeatherCast.Sys.Sunset,
                        currentWeatherCast.Dt,
                        currentWeatherCast.Timezone);

                    currentWeatherDTO.CityDate = currentDateTime.Item1;
                    currentWeatherDTO.CityTime = currentDateTime.Item2;
                    currentWeatherDTO.IsDayTime = currentDateTime.Item3;

                    //capitalize each word in the city name

                    cityName = CapitalizeText(cityName);
                    currentWeatherDTO.CityName = cityName;

                    return currentWeatherDTO;
                }
            }

            return "api service unavailable";
        }

        private string CapitalizeText(string cityName)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            cityName = textInfo.ToTitleCase(cityName);
            return cityName;
        }

        private Tuple<string, string, bool> GetDateTimeFromEpoch(long sunrise, long sunset, long currentTime, long timezone)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(currentTime + timezone);

            bool isDaytime = currentTime > sunrise && currentTime < sunset;

            var humanReadableDate = dateTimeOffset.DateTime.ToString("D");
            var humanReadableTime = dateTimeOffset.DateTime.ToString("t");

            return Tuple.Create(humanReadableDate, humanReadableTime, isDaytime);
        }
    }
}
