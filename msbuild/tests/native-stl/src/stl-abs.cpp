// abs_main.cpp
// compile with: /EHsc
#include <iostream>                 // for i/o functions
#include <valarray>                 // for valarray
using namespace std;

#define ARRAY_SIZE  10              // array size
typedef valarray<int> INTVALARRAY;  // type for valarray of ints

int main()
{
    int i;
    // Initialize val_array to 0, -1, -2, etc.
    INTVALARRAY val_array(ARRAY_SIZE);
    for (i = 0; i < ARRAY_SIZE; i++)
        val_array[i] = -i;

    // Display the size of val_array.
    cout << "Size of val_array = " << val_array.size() << "\n\n";

    // Display the values of val_array before calling abs().
    cout << "The result of val_array before calling abs():\n";
    for (i = 0; i < ARRAY_SIZE; i++)
    {
        cout << val_array[i];
        if (i < ARRAY_SIZE - 1)
            cout << ", ";
    }
    cout << endl << endl;

    // Display the result of val_array after calling abs().
    INTVALARRAY abs_array = abs(val_array);
    cout << "The result of val_array after calling abs():\n";
    for (i = 0; i < ARRAY_SIZE; i++)
    {
        cout << abs_array[i];
        if (i < ARRAY_SIZE - 1)
            cout << ", ";
    }
    cout << endl;
}