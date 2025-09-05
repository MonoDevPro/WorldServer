AOI Networking

- LiteNetLibAdapter now filters snapshots using an Area of Interest radius configured by SpatialOptions.InterestRadius.
- It tracks last-known positions per CharId based on Join/Move/Teleport snapshots.
- Fallback to broadcast-all is used if spatial index wiring is not available.
