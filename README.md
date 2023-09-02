# Implementation of a URL Based Head Sampler in OpenTelemetry .NET

This is an implementation of a Head Sampler that will sample based on the incoming URL. Further, if the URL is sampled, it will add the sample rate as an attribute called `SampleRate`.
