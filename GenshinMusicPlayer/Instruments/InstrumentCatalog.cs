using System;
using System.Collections.Generic;

namespace GenshinMusicPlayer
{
    public static class InstrumentCatalog
    {
        public static IReadOnlyList<IInstrument> All { get; } = Array.AsReadOnly(new IInstrument[]
        {
            new 风物之诗琴(),
            new 老旧的诗琴(),
            new 镜花之琴(),
            new 晚风圆号(),
            new 悠可琴(),
            new 余音(),
            new 跃律琴(),
            new 沃雅妮莎()
        });
    }
}
