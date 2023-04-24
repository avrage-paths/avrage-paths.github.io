public enum ErrorTypes
{
    //This is when the user specifies numJunctions and uses integer data rep 
    //The sum of junctions doesn't equal the number of junctions specified 
    JuncDistributionDoesntMatchDistance,

    //This is when using a percent dataRep
    //The percents of each junction (using random to fill in gaps) doesn't equal 1 
    JuncDistributionNot100Percent,
}

