using UsenetSharp.Models;

namespace UsenetSharp.Clients;

public partial class UsenetClient
{
    public async Task<UsenetHeadResponse> HeadAsync(SegmentId segmentId, CancellationToken cancellationToken)
    {
        await _commandLock.WaitAsync(cancellationToken);

        try
        {
            ThrowIfUnhealthy();
            ThrowIfNotConnected();

            // Send HEAD command with message-id
            await WriteLineAsync($"HEAD <{segmentId}>".AsMemory(), _cts.Token);
            var response = await ReadLineAsync(_cts.Token);
            var responseCode = ParseResponseCode(response);

            // Article retrieved - head follows (multi-line)
            if (responseCode == (int)UsenetResponseType.ArticleRetrievedHeadFollows)
            {
                // Parse headers
                var headers = await ParseArticleHeadersAsync(_cts.Token);

                return new UsenetHeadResponse
                {
                    SegmentId = segmentId,
                    ResponseCode = responseCode,
                    ResponseMessage = response!,
                    ArticleHeaders = headers
                };
            }

            return new UsenetHeadResponse
            {
                ResponseCode = responseCode,
                ResponseMessage = response!,
                SegmentId = segmentId,
                ArticleHeaders = null
            };
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task<IReadOnlyList<UsenetHeadResponse>> HeadBatchAsync(
        IReadOnlyList<SegmentId> segmentIds,
        CancellationToken cancellationToken)
    {
        if (segmentIds.Count == 0)
            return Array.Empty<UsenetHeadResponse>();

        await _commandLock.WaitAsync(cancellationToken);

        try
        {
            ThrowIfUnhealthy();
            ThrowIfNotConnected();

            for (var index = 0; index < segmentIds.Count; index++)
            {
                var segmentId = segmentIds[index];
                await WriteLineAsync($"HEAD <{segmentId}>".AsMemory(), _cts.Token);
            }

            var responses = new UsenetHeadResponse[segmentIds.Count];
            for (var index = 0; index < segmentIds.Count; index++)
            {
                var segmentId = segmentIds[index];
                var response = await ReadLineAsync(_cts.Token);
                var responseCode = ParseResponseCode(response);

                if (responseCode == (int)UsenetResponseType.ArticleRetrievedHeadFollows)
                {
                    var headers = await ParseArticleHeadersAsync(_cts.Token);
                    responses[index] = new UsenetHeadResponse
                    {
                        SegmentId = segmentId,
                        ResponseCode = responseCode,
                        ResponseMessage = response!,
                        ArticleHeaders = headers
                    };
                    continue;
                }

                responses[index] = new UsenetHeadResponse
                {
                    SegmentId = segmentId,
                    ResponseCode = responseCode,
                    ResponseMessage = response!,
                    ArticleHeaders = null
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
