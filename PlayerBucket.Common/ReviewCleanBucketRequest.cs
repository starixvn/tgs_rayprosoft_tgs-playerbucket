namespace Tgs.Service.PlayerBuckets;
public record ReviewCleanBucketRequest<TGameBalanceReturn>(
	decimal CleaningBalance,
	IEnumerable<TypeBucketResult> CleaningBucketAmounts,
	TGameBalanceReturn? GameBalanceReturn);
