using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Confab.Data.DatabaseModels
{
    [Index(propertyNames: nameof(IPAddressBytes), IsUnique = true)]

    public class ClientIPSchema
    {
        public int Id { get; set; }

        [Required, MinLength(4), MaxLength(16)]
        public byte[] IPAddressBytes { get; set; } //TODO: is another variable needed for ipv4 vs v6?

        [NotMapped]
        public IPAddress IPAddress
        {
            get { return new IPAddress(IPAddressBytes); }
            set { IPAddressBytes = value.GetAddressBytes(); }
        }

        [InverseProperty("CreationIP")]
        public List<UserSchema> CreatedAnonUsers { get; set; } = new List<UserSchema>();

        public bool IsBanned { get; set; }
    }
}