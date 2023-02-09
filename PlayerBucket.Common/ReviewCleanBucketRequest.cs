namespace Tgs.Service.PlayerBuckets;
public record ReviewCleanBucketRequest<TGameBalanceReturn>(
	decimal CleaningBalance,
	decimal CleaningBucketAmount,
	TGameBalanceReturn? GameBalanceReturn);
