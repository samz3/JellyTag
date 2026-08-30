using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;

namespace JellyTag.Controllers;

[ApiController]
[Route("JellyTag")]
[Authorize(Policy = "RequiresElevation")] // Requires admin privileges
public class TagManagerController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;

    public TagManagerController(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    private IReadOnlyList<BaseItem> GetItemListSafe(InternalItemsQuery query)
    {
        var method = _libraryManager.GetType().GetMethod("GetItemList", new[] { typeof(InternalItemsQuery) });
        if (method == null)
        {
            throw new InvalidOperationException("GetItemList method not found on LibraryManager via reflection.");
        }

        var resultObj = method.Invoke(_libraryManager, new object[] { query });
        if (resultObj == null)
        {
            return Array.Empty<BaseItem>();
        }

        if (resultObj is IReadOnlyList<BaseItem> readOnlyList)
        {
            return readOnlyList;
        }

        if (resultObj is IEnumerable<BaseItem> enumerable)
        {
            return enumerable.ToList();
        }

        var itemsProp = resultObj.GetType().GetProperty("Items");
        if (itemsProp != null)
        {
            var itemsVal = itemsProp.GetValue(resultObj);
            if (itemsVal is IReadOnlyList<BaseItem> propList)
            {
                return propList;
            }
            if (itemsVal is IEnumerable<BaseItem> propEnumerable)
            {
                return propEnumerable.ToList();
            }
        }

        throw new InvalidOperationException($"Unsupported return type from GetItemList: {resultObj.GetType().FullName}");
    }

    [HttpGet("Libraries")]
    public ActionResult<List<LibraryDto>> GetLibraries()
    {
        try
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.CollectionFolder },
                IsVirtualItem = false
            };
            
            var libraries = GetItemListSafe(query);
            var result = libraries.Select(x => new LibraryDto
            {
                Id = x.Id,
                Name = x.Name
            }).OrderBy(x => x.Name).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }
    }

    [HttpGet("Movies")]
    public ActionResult<List<MovieDto>> GetMovies(
        [FromQuery] string? searchTerm,
        [FromQuery] string? tagFilter,
        [FromQuery] Guid? libraryId)
    {
        try
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                Recursive = true,
                IsVirtualItem = false
            };

            if (libraryId.HasValue && libraryId.Value != Guid.Empty)
            {
                query.AncestorIds = new[] { libraryId.Value };
            }

            var movies = GetItemListSafe(query);

            IEnumerable<BaseItem> filteredMovies = movies;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredMovies = filteredMovies.Where(x => x.Name != null && x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tagFilter))
            {
                filteredMovies = filteredMovies.Where(x => x.Tags != null && x.Tags.Contains(tagFilter, StringComparer.OrdinalIgnoreCase));
            }

            var result = filteredMovies.Select(x => new MovieDto
            {
                Id = x.Id,
                Name = x.Name ?? "Unknown Movie",
                Tags = x.Tags ?? Array.Empty<string>(),
                Year = x.ProductionYear,
                Path = x.Path ?? string.Empty
            }).OrderBy(x => x.Name).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }
    }

    [HttpPost("Tags/Update")]
    public async Task<IActionResult> UpdateTags([FromBody] UpdateTagsRequest request)
    {
        var item = _libraryManager.GetItemById(request.MovieId);
        if (item == null)
        {
            return NotFound("Movie not found");
        }

        item.Tags = request.Tags;
        
        // Save the modifications back to the repository and notify listeners
        await _libraryManager.UpdateItemAsync(item, item.GetParent(), ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        
        return Ok();
    }

    [HttpPost("Tags/BulkUpdate")]
    public async Task<IActionResult> BulkUpdateTags([FromBody] BulkUpdateTagsRequest request)
    {
        if (request.MovieIds == null || request.MovieIds.Count == 0)
        {
            return BadRequest("No movie IDs provided");
        }

        foreach (var id in request.MovieIds)
        {
            var item = _libraryManager.GetItemById(id);
            if (item == null)
            {
                continue;
            }

            var currentTags = item.Tags?.ToList() ?? new List<string>();

            if (request.Action == BulkAction.Add)
            {
                foreach (var tag in request.Tags)
                {
                    if (!currentTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        currentTags.Add(tag);
                    }
                }
            }
            else if (request.Action == BulkAction.Remove)
            {
                foreach (var tag in request.Tags)
                {
                    currentTags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
                }
            }
            else if (request.Action == BulkAction.Set)
            {
                currentTags = request.Tags.ToList();
            }

            item.Tags = currentTags.ToArray();
            await _libraryManager.UpdateItemAsync(item, item.GetParent(), ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        return Ok();
    }
}

public class LibraryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MovieDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int? Year { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class UpdateTagsRequest
{
    public Guid MovieId { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class BulkUpdateTagsRequest
{
    public List<Guid> MovieIds { get; set; } = new();
    public BulkAction Action { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public enum BulkAction
{
    Add,
    Remove,
    Set
}
