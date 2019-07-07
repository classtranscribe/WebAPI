using System;
using System.Collections.Generic;
using System.Text;

namespace TaskEngine
{
    interface IRabbitMQTask<T>
    {
        public void Publish(T obj);
        public void Consume();
    }

    public enum TaskType
    {
        FetchPlaylistData,
        DownloadMedia,
        ConvertMedia,
        TranscribeMedia
    }
}
