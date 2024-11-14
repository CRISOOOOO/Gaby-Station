﻿using Content.Shared.Blob;
using Content.Shared.Blob.Components;

namespace Content.Client.Blob;

public sealed class BlobCarrierSystem : SharedBlobCarrierSystem
{
    protected override void TransformToBlob(Entity<BlobCarrierComponent> ent)
    {
        // do nothing
    }
}
