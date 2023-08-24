namespace Tgs.Service.PlayerBuckets;
public record ReviewCleanBucketRequest<TGameBalanceReturn>(
	decimal CleaningBalance,
	IEnumerable<CleanFormBucketResult> CleaningBucketAmounts,
	TGameBalanceReturn? GameBalanceReturn);
