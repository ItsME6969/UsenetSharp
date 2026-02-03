using UsenetSharp.Models;

namespace UsenetSharp.Clients;

public partial class UsenetClient
{
    public async Task<UsenetStatResponse> StatAsync(SegmentId segmentId, CancellationToken cancellationToken)
    {
        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            ThrowIfUnhealthy();
            ThrowIfNotConnected();

            // Send STAT command with message-id
            await WriteLineAsync($"STAT <{segmentId}>".AsMemory(), _cts.Token);
            var response = await ReadLineAsync(_cts.Token);
            var responseCode = ParseResponseCode(response);

            return new UsenetStatResponse()
            {
                ResponseCode = responseCode,
                ResponseMessage = response!,
                ArticleExists = responseCode == (int)UsenetResponseType.ArticleExists,
            };
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task<IReadOnlyList<UsenetStatResponse>> StatBatchAsync(
        IReadOnlyList<SegmentId> segmentIds,
        CancellationToken cancellationToken)
    {
        if (segmentIds.Count == 0)
            return Array.Empty<UsenetStatResponse>();

        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            ThrowIfUnhealthy();
            ThrowIfNotConnected();

            for (var index = 0; index < segmentIds.Count; index++)
            {
                var segmentId = segmentIds[index];
                await WriteLineAsync($"STAT <{segmentId}>".AsMemory(), _cts.Token);
            }

            var responses = new UsenetStatResponse[segmentIds.Count];
            for (var index = 0; index < segmentIds.Count; index++)
            {
                var response = await ReadLineAsync(_cts.Token);
                var responseCode = ParseResponseCode(response);
                responses[index] = new UsenetStatResponse()
                {
                    ResponseCode = responseCode,
                    ResponseMessage = response!,
                    ArticleExists = responseCode == (int)UsenetResponseType.ArticleExists,
                };
            }

            return responses;
        }
        finally
        {
            _commandLock.Release();
        }
    }
}
