using System;
using System.Text.Json;
using Refit;

namespace RefitObservableTestPlay
{
    public interface ISomeApiService
    {
        [Get("/v1/api/thing")]
        IObservable<Thing> GetThing();
        
    }
}
