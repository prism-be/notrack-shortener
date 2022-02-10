﻿// -----------------------------------------------------------------------
//  <copyright file="ShortenUrl.cs" company="Prism">
//  Copyright (c) Prism. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace Prism.NoTrack.Shortener.Commands;

using System.ComponentModel.DataAnnotations;

using FluentValidation;

using LiteDB;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nanoid;

using Prism.NoTrack.Shortener.Model;
using Prism.NoTrack.Shortener.Options;

public record ShortenedUrl(string Url);

public record ShortenUrl([Required] string Url) : IRequest<ShortenedUrl?>;

public class ShortenUrlValidator : AbstractValidator<ShortenUrl>
{
    public ShortenUrlValidator()
    {
        this.RuleFor(x => x.Url).NotEmpty().MaximumLength(80000);
    }
}

public class ShortenUrlHandler : IRequestHandler<ShortenUrl, ShortenedUrl?>
{
    private readonly ShortenerConfiguration configuration;
    private readonly ILogger<ShortenUrlHandler> logger;

    public ShortenUrlHandler(ILogger<ShortenUrlHandler> logger, IOptions<ShortenerConfiguration> configuration)
    {
        this.logger = logger;
        this.configuration = configuration.Value;
    }

    public async Task<ShortenedUrl?> Handle(ShortenUrl request, CancellationToken cancellationToken)
    {
        this.logger.LogDebug("Shortening : {request}", request);

        var id = await Nanoid.GenerateAsync(size: 16);

        using var database = new LiteDatabase(this.configuration.ConnectionString);
        var collection = database.GetCollection<Redirection>("customers");

        var redirection = new Redirection(id, request.Url);

        collection.Insert(redirection);
        collection.EnsureIndex(x => x.Id);

        this.logger.LogDebug("The url {url} has been stored with id : {id}", redirection.LongUrl, redirection.Id);
        return new ShortenedUrl($"{this.configuration.ShortDomain?.TrimEnd('/')}/r/{id}");
    }
}