using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Ports.Commons.Persistence;
using Simulation.Domain.Templates;
using Simulation.Persistence.Commons; // verifique: Common vs Commons

namespace Simulation.Persistence.Char
{
    public sealed class CharTemplateRepository : DefaultRepository<int, CharTemplate>, ICharTemplateRepository, IInitializable
    {
        private readonly ICharTemplateIndex _charTemplateIndex;
        private readonly ILogger<CharTemplateRepository> _logger;

        public CharTemplateRepository(
            ICharTemplateIndex charTemplateIndex,
            ILogger<CharTemplateRepository> logger) : base(enableReverseLookup: false)
        {
            _charTemplateIndex = charTemplateIndex ?? throw new ArgumentNullException(nameof(charTemplateIndex));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Inicializa o repositório (seed). Chame durante a startup.
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.LogInformation("CharTemplateRepository: Initialization started.");
                SeedChars(_charTemplateIndex);
                var total = this.GetAll().Count();
                _logger.LogInformation("CharTemplateRepository: Initialization completed. Templates seeded: {Count}", total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CharTemplateRepository: Initialization failed.");
                throw;
            }
        }

        private void SeedChars(ICharTemplateIndex charTemplateIndex)
        {
            _logger.LogDebug("SeedChars: Starting seed of character templates.");

            var chars = new List<CharTemplate>
            {
                new() { CharId = 1, Name = "Filipe",  MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
                new() { CharId = 2, Name = "Rodorfo", MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            };

            foreach (var ch in chars)
            {
                if (!charTemplateIndex.TryGet(ch.CharId, out var existing))
                {
                    charTemplateIndex.Register(ch.CharId, ch);
                    _logger.LogInformation("SeedChars: Registered CharTemplate in ICharTemplateIndex (CharId={CharId}, Name={Name}).", ch.CharId, ch.Name);
                }
                else
                {
                    _logger.LogDebug("SeedChars: ICharTemplateIndex already had CharId={CharId}; skipping register.", ch.CharId);
                }

                if (!this.TryGet(ch.CharId, out var _))
                {
                    this.Add(ch.CharId, ch);
                    _logger.LogInformation("SeedChars: Added CharTemplate to repository (CharId={CharId}).", ch.CharId);
                }
                else
                {
                    _logger.LogDebug("SeedChars: Repository already contains CharId={CharId}; skipping add.", ch.CharId);
                }
            }

            _logger.LogDebug("SeedChars: Finished seeding character templates.");
        }
    }
}
