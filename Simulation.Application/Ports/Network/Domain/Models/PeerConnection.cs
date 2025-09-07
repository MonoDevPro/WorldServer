// using System;

// namespace NetworkHexagonal.Core.Domain.Models;

// /// <summary>
// /// Represents a connection to a peer.
// /// </summary>
// public class PeerConnection
// {
//     /// <summary>
//     /// Gets or sets the unique identifier of the peer.
//     /// </summary>
//     public Guid PeerId { get; set; }

//     /// <summary>
//     /// Gets or sets the address of the peer.
//     /// </summary>
//     public string Address { get; set; }

//     /// <summary>
//     /// Gets or sets the connection status of the peer.
//     /// </summary>
//     public bool IsConnected { get; set; }

//     /// <summary>
//     /// Initializes a new instance of the <see cref="PeerConnection"/> class.
//     /// </summary>
//     /// <param name="peerId">The unique identifier of the peer.</param>
//     /// <param name="address">The address of the peer.</param>
//     /// <param name="port">The port of the peer.</param>
//     /// <param name="isConnected">The connection status of the peer.</param>
//     public PeerConnection(Guid peerId, string address, bool isConnected)
//     {
//         PeerId = peerId;
//         Address = address;
//         IsConnected = isConnected;
//     }
//     /// <summary>
//     /// Initializes a new instance of the <see cref="PeerConnection"/> class.
//     /// </summary>
//     /// <param name="peerId">The unique identifier of the peer.</param>
//     /// <param name="address">The address of the peer.</param>
//     /// <param name="port">The port of the peer.</param>
//     public PeerConnection(Guid peerId, string address)
//         : this(peerId, address, false)
//     {
//     }
// }