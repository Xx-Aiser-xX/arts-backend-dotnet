using Arts.Application.DTOs.Requests;
using Arts.Application.DTOs.Requests.Comments;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Comments;
using Arts.Application.Exceptions;
using Arts.Application.Interfaces;
using Arts.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arts.Application.Services.Comments;

public class CommentService(
    IArtsDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<CommentService> logger) : ICommentService
{
    public async Task<CommentResponse> CreateAsync(CommentCreateRequest request, CancellationToken ct = default)
    {
        var user = await currentUserService.GetRequiredUserAsync(ct);

        var art = await dbContext.Arts
            .FirstOrDefaultAsync(a => a.Id == request.ArtId, ct)
            ?? throw new KeyNotFoundException("Арт не найден");

        var comment = new Comment(request.Text, art, user);

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(ct);

        return comment.Adapt<CommentResponse>();
    }

    public async Task<CommentResponse> UpdateAsync(CommentUpdateRequest request, CancellationToken ct = default)
    {
        var currentUserId = currentUserService.GetRequiredUserId();

        
        var comment = await dbContext.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, ct )
            ?? throw new KeyNotFoundException("Комментарий не найден");

        if (comment.AuthorId != currentUserId)
        {
            logger.LogWarning("Пользователь {UserId} попытался отредактировать чужой комментарий {CommentId} (Автор: {AuthorId})", 
                currentUserId, comment.Id, comment.AuthorId);
            throw new ForbiddenException("Вы не являетесь владельцем комментария");
        }

        comment.UpdateDetails(request.Text);

        await dbContext.SaveChangesAsync(ct);

        return comment.Adapt<CommentResponse>();
    }

    public async Task<Guid> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var currentUserId = currentUserService.GetRequiredUserId();

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Комментарий не найден");

        if (comment.AuthorId != currentUserId)
        {
            logger.LogWarning("Пользователь {UserId} попытался удалить чужой комментарий {CommentId}", 
                currentUserId, comment.Id);
            throw new ForbiddenException("Вы не являетесь владельцем комментария");
        }

        comment.MarkAsDeleted();

        await dbContext.SaveChangesAsync(ct);
        return comment.ArtId; 
    }

    public async Task<PagedResponse<CommentResponse>> GetCommentsByArtIdAsync(Guid artId, int page, int size, CancellationToken ct = default)
    {
        var query = dbContext.Comments.Where(c => c.ArtId == artId);

        var totalElements = await query.CountAsync(ct);

        var comments = await query
            .OrderByDescending(c => c.PublicationTime)
            .Skip((page - 1) * size)
            .Take(size)
            .ProjectToType<CommentResponse>() 
            .ToListAsync(ct);

        return new PagedResponse<CommentResponse>(comments, totalElements, page, size);
    }
}